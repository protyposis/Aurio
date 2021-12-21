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

namespace Aurio.LibSampleRate
{
    /// <summary>
    /// Secret Rabbit Code has a number of different converters which can be selected using the converter_type parameter when calling 
    /// src_simple or src_new. Currently, the five converters available are: 
    /// http://www.mega-nerd.com/SRC/api_misc.html#Converters
    /// </summary>
    public enum ConverterType : int
    {
        /// <summary>
        /// This is a bandlimited interpolator derived from the mathematical sinc function and this is the highest quality 
        /// sinc based converter, providing a worst case Signal-to-Noise Ratio (SNR) of 97 decibels (dB) at a bandwidth of 97%. 
        /// All three SRC_SINC_* converters are based on the techniques of Julius O. Smith although this code was developed 
        /// independantly. 
        /// </summary>
        SRC_SINC_BEST_QUALITY = 0,

        /// <summary>
        /// This is another bandlimited interpolator much like the previous one. It has an SNR of 97dB and a bandwidth of 90%. 
        /// The speed of the conversion is much faster than the previous one. 
        /// </summary>
        SRC_SINC_MEDIUM_QUALITY = 1,

        /// <summary>
        /// This is the fastest bandlimited interpolator and has an SNR of 97dB and a bandwidth of 80%. 
        /// </summary>
        SRC_SINC_FASTEST = 2,

        /// <summary>
        /// A Zero Order Hold converter (interpolated value is equal to the last value). 
        /// The quality is poor but the conversion speed is blindlingly fast. 
        /// </summary>
        SRC_ZERO_ORDER_HOLD = 3,

        /// <summary>
        /// A linear converter. Again the quality is poor, but the conversion speed is blindingly fast. 
        /// </summary>
        SRC_LINEAR = 4
    }
}
