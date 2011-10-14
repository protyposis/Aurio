using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Matching {
    public class AnalysisFunctions {

        private static WindowFunction windowFunction = null;

        /// <summary>
        /// Calculates the similarity of two waves by calculating the differences of all sample pairs, summing
        /// them up and dividing them through the number of samples.
        /// If both waves are similar, they sum to zero (destructive interference), which means they're 100% similar.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>a number between 0 and 1, where 0 means 0% similarity, and 1 means 100% similarity</returns>
        public unsafe static double DestructiveInterference(byte[] x, byte[] y) {
            if (x.Length != y.Length) {
                throw new ArgumentException("interval lengths do not match");
            }
            fixed (byte* xB = &x[0], yB = &y[0]) {
                float* xF = (float*)xB;
                float* yF = (float*)yB;
                int n = x.Length / sizeof(float);

                /* Calculate the mean of the two series x[], y[] */
                double mx = 0;
                double my = 0;
                for (int i = 0; i < n; i++) {
                    mx += xF[i];
                    my += yF[i];
                }
                mx /= n;
                my /= n;

                double diff;
                double result = 0;
                for (int i = 0; i < n; i++) {
                    // remove an eventually existing offset by subtracting the mean
                    diff = ((xF[i] - mx) - (yF[i] - my));
                    result += diff > 0 ? diff : -diff;
                }

                return 1 - result / n;
            }
        }

        public static double FrequencyDistribution(byte[] x, byte[] y) {
            if (x.Length != y.Length) {
                throw new ArgumentException("interval lengths do not match");
            }

            int samples = x.Length / 4;
            float[] xF = new float[samples];
            float[] yF = new float[samples];

            Buffer.BlockCopy(x, 0, xF, 0, x.Length);
            Buffer.BlockCopy(y, 0, yF, 0, y.Length);

            int fftSize = 2048; // max FFT size
            // if there are less samples to analyze, find a fitting FFT size
            while (fftSize > samples) {
                fftSize /= 2;
            }
            int hopSize = fftSize / 8;

            if (fftSize < 64) {
                throw new Exception("FFT might be too small to get a meaningful result");
            }

            double[] xSum = new double[fftSize / 2];
            double[] ySum = new double[fftSize / 2];
            float[] buffer = new float[fftSize];

            if (windowFunction == null || windowFunction.Size != fftSize) {
                windowFunction = WindowUtil.GetFunction(WindowType.Hann, fftSize);
            }

            int blocks = (samples - fftSize) / hopSize;
            for (int block = 0; block < blocks; block++) {
                Array.Copy(xF, block * hopSize, buffer, 0, buffer.Length);
                FrequencyDistributionBlockProcess(buffer, xSum);

                Array.Copy(yF, block * hopSize, buffer, 0, buffer.Length);
                FrequencyDistributionBlockProcess(buffer, ySum);
            }

            // remove DC offset
            xSum[0] = 0;
            ySum[0] = 0;

            double xScalarSum = xSum.Sum();
            double yScalarSum = ySum.Sum();

            double result = 0;
            for (int i = 0; i < fftSize / 2; i++) {
                result += Math.Abs(xSum[i] / xScalarSum - ySum[i] / yScalarSum);
            }

            return 1 - result;
        }

        private static void FrequencyDistributionBlockProcess(float[] buffer, double[] target) {
            windowFunction.Apply(buffer);
            FFTUtil.FFT(buffer);
            for (int i = 0; i < buffer.Length; i += 2) {
                buffer[i / 2] = FFTUtil.CalculateMagnitude(buffer[i], buffer[i + 1]) / buffer.Length * 2;
            }
            for (int i = 0; i < target.Length; i++) {
                target[i] += buffer[i];
            }
        }
    }
}
