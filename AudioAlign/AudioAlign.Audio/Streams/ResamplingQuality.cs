using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    /// <summary>
    /// Equivalent to AudioAlign.LibSampleRate.ConverterType.
    /// Introduced to eliminate direct dependencies to AudioAlign.LibSampleRate of executables that use AudioAlign.Audio.
    /// </summary>
    public enum ResamplingQuality {
        SincBest = 0,
        SincMedium = 1,
        SincFastest = 2,
        ZeroOrderHold = 3,
        Linear = 4
    }
}
