using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    /// <summary>
    /// Volume calculations.
    /// </summary>
    /// <seealso cref="NAudio.Utils.Decibels"/>
    /// <seealso cref="http://stackoverflow.com/questions/6627288/audio-spectrum-analysis-using-fft-algorithm-in-java"/>
    public static class VolumeUtil {

        private static readonly double LOG_2_DB;
        private static readonly double DB_2_LOG;

        static VolumeUtil() {
            // precalculate factors to speed up calculations
            LOG_2_DB = 20 / Math.Log(10);
            DB_2_LOG = Math.Log(10) / 20;
        }

        /// <summary>
        /// Converts a linear value (e.g. a sample value) to decibel.
        /// decibel = 20 * ln(linearValue) / ln(10) = 20 * log10(linearValue)
        /// </summary>
        /// <param name="linear">linear value</param>
        /// <returns>decibel value</returns>
        public static double LinearToDecibel(double linear) {
            //return 20 * Math.Log10(linear);
            return Math.Log(linear) * LOG_2_DB;
        }

        /// <summary>
        /// Converts decibel to a linear value.
        /// </summary>
        /// <param name="decibel">decibel value</param>
        /// <returns>linear value</returns>
        public static double DecibelToLinear(double decibel) {
            return Math.Exp(decibel * DB_2_LOG);
        }

        /// <summary>
        /// Calculates the percentage of a decibel value where minDecibel is 0.0 (0%) and maxDecibel is 1.0 (100%).
        /// </summary>
        /// <param name="decibel">decibel value of which the percentage should be calculated</param>
        /// <param name="minDecibel">lower bound os the decibel range (0%)</param>
        /// <param name="maxDecibel">upper bound of the decibel range (100%)</param>
        /// <returns>the percentage of a decibel value between two bounding decibel values</returns>
        public static double DecibelToPercentage(double decibel, double minDecibel, double maxDecibel) {
            return (decibel - minDecibel) / (maxDecibel - minDecibel);
        }
    }
}
