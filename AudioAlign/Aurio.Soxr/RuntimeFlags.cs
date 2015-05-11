using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Soxr {
    [Flags]
    public enum RuntimeFlags : uint {
        /// <summary>
        /// Auto select coef. interpolation.
        /// </summary>
        SOXR_COEF_INTERP_AUTO = 0,
        /// <summary>
        /// Man. select: less CPU, more memory.
        /// </summary>
        SOXR_COEF_INTERP_LOW = 2,
        /// <summary>
        /// Man. select: more CPU, less memory.
        /// </summary>
        SOXR_COEF_INTERP_HIGH = 3
    }
}
