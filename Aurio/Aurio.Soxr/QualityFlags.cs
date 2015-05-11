using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Soxr {
    [Flags]
    public enum QualityFlags : uint {
        /// <summary>
        /// <= 0.01 dB
        /// </summary>
        SOXR_ROLLOFF_SMALL = 0,
        /// <summary>
        /// <= 0.35 dB
        /// </summary>
        SOXR_ROLLOFF_MEDIUM = 1,
        /// <summary>
        /// For Chebyshev bandwidth
        /// </summary>
        SOXR_ROLLOFF_NONE = 2,
        /// <summary>
        /// Increase 'irrational' ratio accuracy
        /// </summary>
        SOXR_HI_PREC_CLOCK = 8,
        /// <summary>
        /// Use D.P. calcs even if precision <= 20
        /// </summary>
        SOXR_DOUBLE_PRECISION = 16,
        /// <summary>
        /// Experimental, variable-rate resampling
        /// </summary>
        SOXR_VR = 32
    }
}
