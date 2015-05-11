using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio {
    public static class FFTUtil {

        /// <summary>
        /// Removes the DC offset of a set of samples by subtracting the average value of all samples
        /// from each sample. 
        /// Use it to preprocess FFT input to eliminate high-level 0Hz components in FFT results.
        /// See: Windowing Functions Improve FFT Results, Part II (http://www.tmworld.com/article/325630-Windowing_Functions_Improve_FFT_Results_Part_II.php)
        /// </summary>
        /// <param name="samples">the samples whose DC offset should be removed</param>
        public static void EliminateDCOffset(float[] samples) {
            float average = samples.Average();
            for (int x = 0; x < samples.Length; x++) {
                samples[x] -= average;
            }
        }

        /// <summary>
        /// Calculates the magnitude of a complex FFT output bin.
        /// </summary>
        public static float CalculateMagnitude(float re, float im) {
            // calculate magnitude of a FFT bin (L2 norm)
            return (float)Math.Sqrt(re * re + im * im);
        }

        public static void CalculateMagnitudes(float[] complexFFTOutput, float[] resultMagnitudes) {
            int y = 0;
            for (int x = 0; x < complexFFTOutput.Length; x += 2) {
                resultMagnitudes[y] = CalculateMagnitude(complexFFTOutput[x], complexFFTOutput[x + 1]);
                y++;
            }
        }

        /// <summary>
        /// Calculates the squared magnitude of a complex FFT output bin. Can be used in conjunction with
        /// decibel conversion to avoid the square root calculation and double->float cast.
        /// </summary>
        public static float CalculateMagnitudeSquared(float re, float im) {
            return re * re + im * im;
        }

        public static void CalculateMagnitudesSquared(float[] complexFFTOutput, float[] resultMagnitudes) {
            int y = 0;
            for (int x = 0; x < complexFFTOutput.Length; x += 2) {
                resultMagnitudes[y] = CalculateMagnitudeSquared(complexFFTOutput[x], complexFFTOutput[x + 1]);
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
        public static void NormalizeResults(float[] fftOutput, float[] normalizedResult) {
            float max = float.MinValue;
            int y = 0;
            for (int x = 0; x < fftOutput.Length; x += 2) {
                // calculate magnitude of a FFT bin (L2 norm)
                normalizedResult[y] = CalculateMagnitude(fftOutput[x], fftOutput[x + 1]);
                // find out max value for normalization
                if (normalizedResult[y] > max) {
                    max = normalizedResult[y];
                }
                y++;
            }
            for (int x = 0; x < normalizedResult.Length; x++) {
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
        public static int Results(float[] fftOutput, float[] result) {
            return Results(fftOutput, result, 0f);
        }

        public static int Results(float[] fftOutput, float[] result, float decibelOffset) {
            float max = float.MinValue;
            int peak = -1;
            int y = 0;
            for (int x = 0; x < fftOutput.Length; x += 2) {
                // calculate magnitude of a FFT bin (L2 norm)
                // divide magnitudes by FFT input length (so that they aren't dependent in the input length)
                // multiply by 2 since the FFT result only contains half of the energy (the second half are the negative frequencies of the "full" FFT result)
                // calculate dB scale value
                // http://www.mathworks.de/support/tech-notes/1700/1702.html
                result[y] = (float)VolumeUtil.LinearToDecibel(
                    CalculateMagnitude(fftOutput[x], fftOutput[x + 1]) / fftOutput.Length * 2) + decibelOffset;
                if (result[y] > max) {
                    max = result[y];
                    peak = y;
                }
                if (float.IsNegativeInfinity(result[y])) {
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
        public static double[] CalculateFrequencyBoundariesLog(int minFrequency, int maxFrequency, int numBands) {
            double logRatio = Math.Log((double)(maxFrequency) / (double)(minFrequency));
            double x = Math.Exp(logRatio / numBands);
            double[] freqs = new double[numBands + 1];
            freqs[0] = minFrequency;
            for (int i = 1; i <= numBands; i++) {
                freqs[i] = freqs[i - 1] * x;
            }
            return freqs;
        }

        public static double[] CalculateFrequencyBoundariesLinear(int minFrequency, int maxFrequency, int numBands) {
            double[] freqs = new double[numBands + 1];
            double width = (maxFrequency - minFrequency) / (double)numBands;
            freqs[0] = minFrequency;
            for (int i = 1; i <= numBands; i++) {
                freqs[i] = freqs[i - 1] + width;
            }
            return freqs;
        }
    }
}
