//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Linq;
using Aurio.FFT;

namespace Aurio.Matching
{
    public class AnalysisFunctions
    {
        private static WindowFunction windowFunction = null;

        public static unsafe double CrossCorrelationOffset(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
            {
                throw new ArgumentException("interval lengths do not match");
            }
            fixed (
                byte* xB = &x[0],
                    yB = &y[0]
            )
            {
                float* xF = (float*)xB;
                float* yF = (float*)yB;
                int n = x.Length / sizeof(float);
                CrossCorrelation.Result ccr;
                return (1 - Math.Abs(CrossCorrelation.Calculate(xF, yF, n, out ccr)) / (n / 2d))
                    * ccr.AbsoluteMaxValue;
            }
        }

        /// <summary>
        /// Calculates the similarity of two waves by calculating the differences of all sample pairs, summing
        /// them up and dividing them through the number of samples.
        /// If both waves are similar, they sum to zero (destructive interference), which means they're 100% similar.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>a number between 0 and 1, where 0 means 0% similarity, and 1 means 100% similarity</returns>
        public unsafe static double DestructiveInterference(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
            {
                throw new ArgumentException("interval lengths do not match");
            }
            fixed (
                byte* xB = &x[0],
                    yB = &y[0]
            )
            {
                float* xF = (float*)xB;
                float* yF = (float*)yB;
                int n = x.Length / sizeof(float);

                /* Calculate the mean of the two series x[], y[] */
                double mx = 0;
                double my = 0;
                for (int i = 0; i < n; i++)
                {
                    mx += xF[i];
                    my += yF[i];
                }
                mx /= n;
                my /= n;

                double diff;
                double result = 0;
                for (int i = 0; i < n; i++)
                {
                    // remove an eventually existing offset by subtracting the mean
                    diff = ((xF[i] - mx) - (yF[i] - my));
                    result += diff > 0 ? diff : -diff;
                }

                return 1 - result / n;
            }
        }

        public static double FrequencyDistribution(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
            {
                throw new ArgumentException("interval lengths do not match");
            }

            int samples = x.Length / 4;
            float[] xF = new float[samples];
            float[] yF = new float[samples];

            Buffer.BlockCopy(x, 0, xF, 0, x.Length);
            Buffer.BlockCopy(y, 0, yF, 0, y.Length);

            int fftSize = 2048; // max FFT size
            // if there are less samples to analyze, find a fitting FFT size
            while (fftSize > samples)
            {
                fftSize /= 2;
            }
            int hopSize = fftSize / 8;

            if (fftSize < 64)
            {
                throw new Exception("FFT might be too small to get a meaningful result");
            }

            double[] xSum = new double[fftSize / 2];
            double[] ySum = new double[fftSize / 2];
            float[] buffer = new float[fftSize];

            if (windowFunction == null || windowFunction.Size != fftSize)
            {
                windowFunction = WindowUtil.GetFunction(WindowType.Hann, fftSize);
            }

            var fft = FFTFactory.CreateInstance(fftSize);

            int blocks = (samples - fftSize) / hopSize;
            for (int block = 0; block < blocks; block++)
            {
                Array.Copy(xF, block * hopSize, buffer, 0, buffer.Length);
                FrequencyDistributionBlockProcess(buffer, xSum, fft);

                Array.Copy(yF, block * hopSize, buffer, 0, buffer.Length);
                FrequencyDistributionBlockProcess(buffer, ySum, fft);
            }

            fft.Dispose();

            // remove DC offset
            xSum[0] = 0;
            ySum[0] = 0;

            double xScalarSum = xSum.Sum();
            double yScalarSum = ySum.Sum();

            double result = 0;
            for (int i = 0; i < fftSize / 2; i++)
            {
                result += Math.Abs(xSum[i] / xScalarSum - ySum[i] / yScalarSum);
            }

            return 1 - result;
        }

        private static void FrequencyDistributionBlockProcess(
            float[] buffer,
            double[] target,
            IFFT fft
        )
        {
            windowFunction.Apply(buffer);
            fft.Forward(buffer);
            for (int i = 0; i < buffer.Length; i += 2)
            {
                buffer[i / 2] =
                    FFTUtil.CalculateMagnitude(buffer[i], buffer[i + 1]) / buffer.Length * 2;
            }
            for (int i = 0; i < target.Length; i++)
            {
                target[i] += buffer[i];
            }
        }
    }
}
