using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;

namespace AudioAlign.Audio {
    public static class FFTUtil {

        /// <summary>
        /// Removes the DC offset of a set of sampley by subtracting the average value of all samples
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
        /// Normalizes the vertical-axis scale of a FFT result and transforms it to a logarithmic dB
        /// scale for a better visualization. The resulting dB values aren't absolute, but relative to the
        /// main peak (highest peak).
        /// See: Windowing Functions Improve FFT Results, Part II (http://www.tmworld.com/article/325630-Windowing_Functions_Improve_FFT_Results_Part_II.php)
        /// </summary>
        /// <param name="fftOutput">the output of a FFT function (interleaved complex numbers)</param>
        /// <param name="normalizedResult">the normalized result for visualization</param>
        public static void NormalizeResults(float[] fftOutput, float[] normalizedResult) {
            float max = float.MinValue;
            for (int x = 0; x < fftOutput.Length; x += 2) {
                // calculate magnitude of a FFT bin (L2 norm)
                normalizedResult[x / 2] = (float)Math.Sqrt(fftOutput[x] * fftOutput[x] + fftOutput[x + 1] * fftOutput[x + 1]);
                // find out max value for normalization
                if (normalizedResult[x / 2] > max) {
                    max = normalizedResult[x / 2];
                }
            }
            for (int x = 0; x < normalizedResult.Length; x++) {
                // normalize by max value & calculate dB scale value
                normalizedResult[x] = (float)VolumeUtil.LinearToDecibel(normalizedResult[x] / max);
            }
        }

        public static void FFT(float[] values) {
            Fourier.RFFT(values, FourierDirection.Forward);
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
