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

namespace Aurio
{
    public static class FFTUtil
    {
        /// <summary>
        /// Removes the DC offset of a set of samples by subtracting the average value of all samples
        /// from each sample.
        /// Use it to preprocess FFT input to eliminate high-level 0Hz components in FFT results.
        /// See: Windowing Functions Improve FFT Results, Part II (http://www.tmworld.com/article/325630-Windowing_Functions_Improve_FFT_Results_Part_II.php)
        /// </summary>
        /// <param name="samples">the samples whose DC offset should be removed</param>
        public static void EliminateDCOffset(float[] samples)
        {
            float average = samples.Average();
            for (int x = 0; x < samples.Length; x++)
            {
                samples[x] -= average;
            }
        }

        /// <summary>
        /// Calculates the magnitude of a complex FFT output bin.
        /// </summary>
        public static float CalculateMagnitude(float re, float im)
        {
            // calculate magnitude of a FFT bin (L2 norm)
            return (float)new Complex(re, im).Magnitude;
        }

        public static void CalculateMagnitudes(
            float[] complexFFTOutput,
            float[] resultMagnitudes,
            int resultMagnitudesOffset
        )
        {
            int y = 0;
            for (int x = 0; x < complexFFTOutput.Length; x += 2)
            {
                resultMagnitudes[resultMagnitudesOffset + y] = CalculateMagnitude(
                    complexFFTOutput[x],
                    complexFFTOutput[x + 1]
                );
                y++;
            }
        }

        public static void CalculateMagnitudes(float[] complexFFTOutput, float[] resultMagnitudes)
        {
            CalculateMagnitudes(complexFFTOutput, resultMagnitudes, 0);
        }

        public static float CalculatePhase(float re, float im)
        {
            return (float)new Complex(re, im).Phase;
        }

        public static void CalculatePhases(
            float[] complexFFTOutput,
            float[] resultPhases,
            int resultPhasesOffset
        )
        {
            int y = 0;
            for (int x = 0; x < complexFFTOutput.Length; x += 2)
            {
                resultPhases[resultPhasesOffset + y] = CalculatePhase(
                    complexFFTOutput[x],
                    complexFFTOutput[x + 1]
                );
                y++;
            }
        }

        public static void CalculatePhases(float[] complexFFTOutput, float[] resultPhases)
        {
            CalculatePhases(complexFFTOutput, resultPhases, 0);
        }

        public static void MagnitudesAndPhasesToFFT(
            float[] magnitudes,
            int magnitudesOffset,
            float[] phases,
            int phasesOffset,
            float[] fftResult,
            int fftResultOffset,
            int count
        )
        {
            if (magnitudes.Length < magnitudesOffset + count)
            {
                throw new ArgumentOutOfRangeException("magnitudes");
            }
            if (phases.Length < phasesOffset + count)
            {
                throw new ArgumentOutOfRangeException("phases");
            }
            if (fftResult.Length < count * 2)
            {
                throw new ArgumentOutOfRangeException("FFT result");
            }

            for (int i = 0; i < count; i++)
            {
                var c = Complex.FromMagnitudeAndPhase(
                    magnitudes[magnitudesOffset + i],
                    phases[phasesOffset + i]
                );
                fftResult[fftResultOffset + 2 * i + 0] = (float)c.Real;
                fftResult[fftResultOffset + 2 * i + 1] = (float)c.Imaginary;
            }
        }

        /// <summary>
        /// Calculates the squared magnitude of a complex FFT output bin. Can be used in conjunction with
        /// decibel conversion to avoid the square root calculation and double->float cast.
        /// </summary>
        public static float CalculateMagnitudeSquared(float re, float im)
        {
            return re * re + im * im;
        }

        public static void CalculateMagnitudesSquared(
            float[] complexFFTOutput,
            float[] resultMagnitudes
        )
        {
            int y = 0;
            for (int x = 0; x < complexFFTOutput.Length; x += 2)
            {
                resultMagnitudes[y] = CalculateMagnitudeSquared(
                    complexFFTOutput[x],
                    complexFFTOutput[x + 1]
                );
                y++;
            }
        }

        /// <summary>
        /// Normalizes the vertical-axis scale of a FFT result and transforms it to a logarithmic dB
        /// scale for a better visualization. The resulting dB values aren't absolute, but relative to the
        /// main peak (highest peak).
        /// See: Windowing Functions Improve FFT Results, Part II (http://www.tmworld.com/article/325630-Windowing_Functions_Improve_FFT_Results_Part_II.php)
        /// </summary>
        /// <param name="fftOutput">the output of a FFT function (interleaved complex numbers)</param>
        /// <param name="normalizedResult">the normalized result for visualization</param>
        public static void NormalizeResults(float[] fftOutput, float[] normalizedResult)
        {
            float max = float.MinValue;
            int y = 0;
            for (int x = 0; x < fftOutput.Length; x += 2)
            {
                // calculate magnitude of a FFT bin (L2 norm)
                normalizedResult[y] = CalculateMagnitude(fftOutput[x], fftOutput[x + 1]);
                // find out max value for normalization
                if (normalizedResult[y] > max)
                {
                    max = normalizedResult[y];
                }
                y++;
            }
            for (int x = 0; x < normalizedResult.Length; x++)
            {
                // normalize by max value & calculate dB scale value
                normalizedResult[x] = (float)VolumeUtil.LinearToDecibel(normalizedResult[x] / max);
            }
        }

        /// <summary>
        /// Transforms a complex FFT output to a logarithmic dB scale for better visualization, without
        /// normalizing the peak to 0 dB.
        /// </summary>
        /// <param name="fftOutput">the output of a FFT function (interleaved complex numbers)</param>
        /// <param name="result">the result for visualization</param>
        /// <returns>the peak index of the result</returns>
        public static int Results(float[] fftOutput, float[] result)
        {
            return Results(fftOutput, result, 0f);
        }

        public static int Results(float[] fftOutput, float[] result, float decibelOffset)
        {
            float max = float.MinValue;
            int peak = -1;
            int y = 0;
            for (int x = 0; x < fftOutput.Length; x += 2)
            {
                // calculate magnitude of a FFT bin (L2 norm)
                // divide magnitudes by FFT input length (so that they aren't dependent on the input length)
                // multiply by 2 since the FFT result only contains half of the energy (the second half are the negative frequencies of the "full" FFT result)
                // calculate dB scale value
                // http://www.mathworks.de/support/tech-notes/1700/1702.html
                result[y] =
                    (float)
                        VolumeUtil.LinearToDecibel(
                            CalculateMagnitude(fftOutput[x], fftOutput[x + 1])
                                / fftOutput.Length
                                * 2
                        ) + decibelOffset;
                if (result[y] > max)
                {
                    max = result[y];
                    peak = y;
                }
                if (float.IsNegativeInfinity(result[y]))
                {
                    result[y] = float.MinValue;
                }
                y++;
            }
            return peak;
        }

        /// <summary>
        /// Calculates a number of logarithmically distributed frequency bands between a minimum and maximum frequency.
        /// source: http://www.cs.cmu.edu/~yke/musicretrieval/ FFTDisplayPanel.java
        /// </summary>
        /// <param name="minFrequency">the minimum frequency</param>
        /// <param name="maxFrequency">the maximum frequency</param>
        /// <param name="numBands">the number of bands to calculate between the min and max frequency</param>
        /// <returns>an array with numBands + 1 values each representing subsequentially the lower and upper frequency of a band</returns>
        public static double[] CalculateFrequencyBoundariesLog(
            int minFrequency,
            int maxFrequency,
            int numBands
        )
        {
            double logRatio = Math.Log((double)(maxFrequency) / (double)(minFrequency));
            double x = Math.Exp(logRatio / numBands);
            double[] freqs = new double[numBands + 1];
            freqs[0] = minFrequency;
            for (int i = 1; i <= numBands; i++)
            {
                freqs[i] = freqs[i - 1] * x;
            }
            return freqs;
        }

        public static double[] CalculateFrequencyBoundariesLinear(
            int minFrequency,
            int maxFrequency,
            int numBands
        )
        {
            double[] freqs = new double[numBands + 1];
            double width = (maxFrequency - minFrequency) / (double)numBands;
            freqs[0] = minFrequency;
            for (int i = 1; i <= numBands; i++)
            {
                freqs[i] = freqs[i - 1] + width;
            }
            return freqs;
        }

        public static int CalculateFrequencyBinIndex(int sampleRate, float frequency, int binCount)
        {
            float floatBinIndex = (float)binCount / sampleRate * frequency;

            // Round to the nearest bin index, that's where most of the energy of the frequency will be
            return (int)Math.Min(Math.Round(floatBinIndex), binCount - 1);
        }

        /// <summary>
        /// Calculate the next power of 2 of the given value. Returns the given value
        /// if it is already a power of 2.
        /// Can be used to determine an FFT size that can hold a specific number of samples.
        /// </summary>
        public static int CalculateNextPowerOf2(int value)
        {
            // http://stackoverflow.com/questions/264080/how-do-i-calculate-the-closest-power-of-2-or-10-a-number-is
            // http://acius2.blogspot.com/2007/11/calculating-next-power-of-2.html
            int value2 = value;
            value2--;
            value2 = (value2 >> 1) | value2;
            value2 = (value2 >> 2) | value2;
            value2 = (value2 >> 4) | value2;
            value2 = (value2 >> 8) | value2;
            value2 = (value2 >> 16) | value2;
            value2++;

            return value2;
        }

        /// <summary>
        /// Determines whether the given value is a power of 2.
        /// Can be used to check whether it is a valid FFT size.
        /// </summary>
        public static bool IsPowerOf2(int value)
        {
            return CalculateNextPowerOf2(value) == value;
        }
    }
}
