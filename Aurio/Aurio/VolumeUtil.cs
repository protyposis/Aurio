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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio {
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
