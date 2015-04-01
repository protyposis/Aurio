using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    /// <summary>
    /// Resampler quality settings.
    /// 
    /// Introduced to eliminate direct dependencies to the used resampler library in AudioAlign.Audio. 
    /// The ResamplingStream maps the quality setting to the according resampling library quality settings.
    /// </summary>
    public enum ResamplingQuality {
        VeryHigh,
        High,
        Medium,
        VeryLow,
        Low,
        VariableRate
    }
}
