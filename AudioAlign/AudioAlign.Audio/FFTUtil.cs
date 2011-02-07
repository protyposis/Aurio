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
    }
}
