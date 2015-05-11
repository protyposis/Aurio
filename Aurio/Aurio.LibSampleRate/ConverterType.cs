using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.LibSampleRate {
    /// <summary>
    /// Secret Rabbit Code has a number of different converters which can be selected using the converter_type parameter when calling 
    /// src_simple or src_new. Currently, the five converters available are: 
    /// http://www.mega-nerd.com/SRC/api_misc.html#Converters
    /// </summary>
    public enum ConverterType : int {
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
