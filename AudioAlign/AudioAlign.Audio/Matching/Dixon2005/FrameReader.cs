using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;
using System.Diagnostics;
using AudioAlign.Audio.Features;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    /// <summary>
    /// Audio frame reader for the OLTW algorithm.
    /// Inspiration taken from reverse engineered MATCH - PerformanceMatcher GetFrame() / ProcessFrame()
    /// </summary>
    class FrameReader : STFT {

        public const int SAMPLERATE = 44100;
        public const int WINDOW_SIZE = 2048; // 46ms @ 44.1kHz
        public const int WINDOW_HOP_SIZE = 882; // 20ms @ 44.1kHz
        public const WindowType WINDOW_TYPE = WindowType.Hamming;
        public const int FRAME_SIZE = 84;

        private int[] frequencyMap;

        public FrameReader(IAudioStream stream)
            : base(stream, WINDOW_SIZE, WINDOW_HOP_SIZE, WINDOW_TYPE, false) {
                if (stream.Properties.SampleRate != SAMPLERATE) {
                    throw new ArgumentException("wrong sample rate");
                }
                if (stream.Properties.Channels != 1) {
                    throw new ArgumentException("only a mono stream is allowed");
                }

                frequencyMap = MakeFrequencyMap();
        }

        private int[] MakeFrequencyMap() {
            int[] map = new int[WINDOW_SIZE / 2];

            // convert FFT result to compacted frame representation
            // linear mapping of first bins up to 370Hz
            for (int i = 0; i < 17; i++) {
                map[i] = i;
            }

            // logarithmic mapping of 370Hz - 12.5kHz
            // NOTE same code pattern as in FingerprintGenerator
            double[] frequencyMidLogBands = FFTUtil.CalculateFrequencyBoundariesLog(370, 12500, 66);
            double bandWidth = SAMPLERATE / 2d / fftFreqBins.Length;
            for (int x = 0; x < frequencyMidLogBands.Length - 1; x++) {
                currentFrame[17 + x] = 0;
                int lowerIndex = (int)(frequencyMidLogBands[x] / bandWidth);
                int upperIndex = (int)(frequencyMidLogBands[x + 1] / bandWidth);
                for (int y = lowerIndex; y < upperIndex; y++) {
                    map[y] = 17 + x;
                }
            }

            // summation of bins above 12.5kHz
            currentFrame[83] = 0;
            for (int i = 580; i < fftFreqBins.Length; i++) {
                map[i] = 83;
            }

            return map;
        }

        private float[] fftFreqBins = new float[WINDOW_SIZE / 2];
        private float[] previousFrame = new float[FRAME_SIZE];
        private float[] currentFrame = new float[FRAME_SIZE];
        private double currentFrameRMS;

        protected override void OnFrameRead(float[] frame) {
            currentFrameRMS = AudioUtil.CalculateRMS(frame);
        }

        public override void ReadFrame(float[] frame) {
            if (frame.Length != FRAME_SIZE) {
                throw new ArgumentException("wrong frame size");
            }

            base.ReadFrame(fftFreqBins);

            Array.Clear(currentFrame, 0, currentFrame.Length);
            for(int i = 0; i < fftFreqBins.Length; i++) {
                currentFrame[frequencyMap[i]] += fftFreqBins[i];
            }

            // calculate final frame representation (spectral difference)
            // "half-wave rectified first order difference"
            // http://www.answers.com/topic/first-order-difference
            // http://www.ltcconline.net/greenl/courses/204/firstOrder/differenceEquations.htm
            // Dixon / Live Tracking of Musical Performances... / formula 5
            float differenceSum = 0.0f;
            for (int i = 0; i < currentFrame.Length; i++) {
                differenceSum += currentFrame[i];
                frame[i] = Math.Max(currentFrame[i] - previousFrame[i], 0);
            }


            if (currentFrameRMS <= 0.0004D) {
                Array.Clear(frame, 0, frame.Length);
            }

            if (differenceSum > 0) { // MATCH normalize1
                for (int m = 0; m < frame.Length; m++) {
                    frame[m] /= differenceSum;
                }
            }

            CommonUtil.Swap<float[]>(ref currentFrame, ref previousFrame);
        }
    }
}
