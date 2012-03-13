using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    class FrameReader : StreamWindower {

        public const int SAMPLERATE = 44100;
        public const int WINDOW_SIZE = 2048; // 46ms @ 44.1kHz
        public const int WINDOW_HOP_SIZE = 882; // 20ms @ 44.1kHz
        public const WindowType WINDOW_TYPE = WindowType.Hamming;
        public const int FRAME_SIZE = 84;

        private WindowFunction windowFunction;
        private float[] frameBuffer;
        private FFTW.FFTW fftw;

        private double[] frequencyMidLogBands;
        private int[] freqMap = new int[9999];

        public FrameReader(IAudioStream stream)
            : base(stream, WINDOW_SIZE, WINDOW_HOP_SIZE) {
                if (stream.Properties.SampleRate != SAMPLERATE) {
                    throw new ArgumentException("wrong sample rate");
                }
                if (stream.Properties.Channels != 1) {
                    throw new ArgumentException("only a mono stream is allowed");
                }
                windowFunction = WindowUtil.GetFunction(WINDOW_TYPE, WindowSize);
                frameBuffer = new float[WindowSize];
                fftw = new FFTW.FFTW(WindowSize);
                this.frequencyMidLogBands = FFTUtil.CalculateFrequencyBoundariesLog(370, 12500, 66);
                makeStandardFrequencyMap(WINDOW_SIZE, SAMPLERATE);
        }

        /// <summary>
        /// taken from MATCH 0.9.2 at.ofai.music.match.PerformanceMatcher:518
        /// </summary>
        /// <param name="paramInt"></param>
        /// <param name="paramFloat"></param>
        protected void makeStandardFrequencyMap(int fftSize, float sampleRate) {
            double binBandwidth = sampleRate / fftSize;
            int i = (int)(2.0D / (Math.Pow(2.0D, 0.08333333333333333D) - 1.0D));
            int j = (int)Math.Round(Math.Log(i * binBandwidth / 440.0D) / Math.Log(2.0D) * 12.0D + 69.0D);

            int k = 0;
            while (k <= i)
                this.freqMap[k++] = k;
            while (k <= fftSize / 2) {
                double d2 = Math.Log(k * binBandwidth / 440.0D) / Math.Log(2.0D) * 12.0D + 69.0D;
                if (d2 > 127.0D)
                    d2 = 127.0D;
                this.freqMap[k++] = (i + (int)Math.Round(d2) - j);
            }
            //this.freqMapSize = (this.freqMap[(k - 1)] + 1);
        }

        private float[] fftFreqBins = new float[WINDOW_SIZE / 2];
        private float[] previousFrame = new float[FRAME_SIZE];
        private float[] currentFrame = new float[FRAME_SIZE];

        public override void ReadFrame(float[] frame) {
            if (frame.Length != FRAME_SIZE) {
                throw new ArgumentException("wrong frame size");
            }

            //base.ReadFrame(fftFreqBins);

            base.ReadFrame(frameBuffer);


            float d = 0;
            double frameRMS = 0;
            for (int j = 0; j < frameBuffer.Length; j++) {
                d = frameBuffer[j];
                frameRMS += d * d;
            }
            frameRMS = Math.Sqrt(frameRMS / frameBuffer.Length);

            // apply window function
            windowFunction.Apply(frameBuffer);

            // do fourier transform
            //FFTUtil.FFT(frameBuffer);
            fftw.Execute(frameBuffer);

            for (int i = 0; i < WINDOW_SIZE; i+=2) {
                fftFreqBins[i / 2] = frameBuffer[i] * frameBuffer[i] + frameBuffer[i+1] * frameBuffer[i+1];
            }

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
            currentFrame[83] = 0;
            for (int i = 580; i < fftFreqBins.Length; i++) {
                currentFrame[83] += fftFreqBins[i];
            }

            float d1 = 0.0f;
            int k;
            //if (this.useSpectralDifference) // default = true
            for (k = 0; k < currentFrame.Length; k++) {
                d1 += currentFrame[k];
                if (currentFrame[k] > previousFrame[k]) {
                    frame[k] = (currentFrame[k] - previousFrame[k]);
                }
                else
                    frame[k] = 0.0f;
            }
            //else {
            //    for (k = 0; k < this.freqMapSize; k++) {
            //        this.frames[i][k] = this.newFrame[k];
            //        d1 += this.frames[i][k];
            //    }
            //}
            //this.frames[i][this.freqMapSize] = d1; // TODO CHECK IF USED - scheinbar nicht

            if (frameRMS <= 0.0004D)
                for (int m = 0; m < frame.Length; m++)
                    frame[m] = 0.0f;
            //else if (this.normalise1) // default = true
            if (d1 > 0) {
                for (int m = 0; m < frame.Length; m++)
                    frame[m] /= d1;
            }

            // calculate final frame representation
            // "half-wave rectified first order difference"
            // http://www.answers.com/topic/first-order-difference
            // http://www.ltcconline.net/greenl/courses/204/firstOrder/differenceEquations.htm
            // Dixon / Live Tracking of Musical Performances... / formula 5
            //for (int i = 0; i < FRAME_SIZE; i++) {
            //    frame[i] = Math.Max(currentFrame[i] - previousFrame[i], 0);
            //}

            CommonUtil.Swap<float[]>(ref currentFrame, ref previousFrame);
        }
    }
}
