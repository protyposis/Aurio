using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    class FrameReader : STFT {

        public const int SAMPLERATE = 44100;
        public const int WINDOW_SIZE = 2048; // 46ms @ 44.1kHz
        public const int WINDOW_HOP_SIZE = 882; // 20ms @ 44.1kHz
        public const WindowType WINDOW_TYPE = WindowType.Hamming;
        public const int FRAME_SIZE = 42;

        private double[] frequencyMidLogBands;

        public FrameReader(IAudioStream stream)
            : base(stream, WINDOW_SIZE, WINDOW_HOP_SIZE, WINDOW_TYPE) {
                if (stream.Properties.SampleRate != SAMPLERATE) {
                    throw new ArgumentException("wrong sample rate");
                }
                if (stream.Properties.Channels != 1) {
                    throw new ArgumentException("only a mono stream is allowed");
                }
                this.frequencyMidLogBands = FFTUtil.CalculateFrequencyBoundariesLog(370, 12500, 24);
        }

        private float[] fftFreqBins = new float[WINDOW_SIZE / 2];
        private float[] previousFrame = new float[FRAME_SIZE];
        private float[] currentFrame = new float[FRAME_SIZE];

        public override void ReadFrame(float[] frame) {
            if (frame.Length != FRAME_SIZE) {
                throw new ArgumentException("wrong frame size");
            }

            base.ReadFrame(fftFreqBins);

            // convert FFT result to compacted frame representation
            // linear mapping of first bins up to 370Hz
            for (int i = 0; i < 17; i++) {
                currentFrame[i] = fftFreqBins[i];
            }

            // logarithmic mapping of 370Hz - 12.5kHz
            // NOTE same code pattern as in FingerprintGenerator
            double bandWidth = SAMPLERATE / 2d / fftFreqBins.Length;
            for (int x = 0; x < frequencyMidLogBands.Length - 1; x++) {
                currentFrame[17 + x] = 0;
                int lowerIndex = (int)(frequencyMidLogBands[x] / bandWidth);
                int upperIndex = (int)(frequencyMidLogBands[x + 1] / bandWidth);
                for (int y = lowerIndex; y < upperIndex; y++) {
                    currentFrame[17 + x] += fftFreqBins[y];
                }
            }

            // summation of bins above 12.5kHz
            for (int i = 580; i < fftFreqBins.Length; i++) {
                currentFrame[41] = fftFreqBins[i];
            }

            // TODO calculate final frame representation
            // "half-wave rectified first order difference"
            // http://www.answers.com/topic/first-order-difference
            // http://www.ltcconline.net/greenl/courses/204/firstOrder/differenceEquations.htm
            // Dixon / Live Tracking of Musical Performances... / formula 5
            for (int i = 0; i < FRAME_SIZE; i++) {
                frame[i] = Max(currentFrame[i] - previousFrame[i], 0);
            }

            CommonUtil.Swap<float[]>(ref currentFrame, ref previousFrame);
        }

        private float Max(float val1, float val2) {
            if (float.IsNaN(val1)) {
                return val2;
            }
            if (float.IsNaN(val2)) {
                return val1;
            }
            return Math.Max(val1, val2);
        }
    }
}
