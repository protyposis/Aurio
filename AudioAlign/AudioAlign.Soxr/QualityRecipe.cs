using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Soxr {
    public enum QualityRecipe : uint {
        /// <summary>
        /// 'Quick' cubic interpolation
        /// </summary>
        SOXR_QQ = 0,
        /// <summary>
        /// 'Low' 16-bit with larger rolloff
        /// </summary>
        SOXR_LQ = 1,
        /// <summary>
        /// 'Medium' 16-bit with medium rolloff.
        /// </summary>
        SOXR_MQ = 2,
        /// <summary>
        /// 'High quality'
        /// </summary>
        SOXR_HQ = SOXR_20_BITQ,
        /// <summary>
        /// 'Very high quality'
        /// </summary>
        SOXR_VHQ = SOXR_28_BITQ,

        SOXR_16_BITQ = 3,
        SOXR_20_BITQ = 4,
        SOXR_24_BITQ = 5,
        SOXR_28_BITQ = 6,
        SOXR_32_BITQ = 7,

        /// <summary>
        /// Libsamplerate equivalent 'Best sinc'
        /// </summary>
        SOXR_LSR0Q = 8,
        /// <summary>
        /// Libsamplerate equivalent 'Medium sinc'
        /// </summary>
        SOXR_LSR1Q = 9,
        /// <summary>
        /// Libsamplerate equivalent 'Fast sinc'
        /// </summary>
        SOXR_LSR2Q = 10,

        SOXR_LINEAR_PHASE = 0x00,
        SOXR_INTERMEDIATE_PHASE = 0x10,
        SOXR_MINIMUM_PHASE = 0x30,
        SOXR_STEEP_FILTER = 0x40
    }
}
