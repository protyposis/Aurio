using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Soxr {
    [Flags]
    public enum IoFlags : uint {
        /// <summary>
        /// Applicable only if otype is INT16.
        /// </summary>
        SOXR_TPDF = 0,
        /// <summary>
        /// Disable the above.
        /// </summary>
        SOXR_NO_DITHER = 8
    }
}
