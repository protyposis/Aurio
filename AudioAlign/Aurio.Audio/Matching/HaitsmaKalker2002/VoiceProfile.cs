using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Matching.HaitsmaKalker2002 {
    class VoiceProfile : DefaultProfile {

        public VoiceProfile() : base() {
            Name = "Voice";

            // vocal range 80 - 1100 Hz: http://en.wikipedia.org/wiki/Vocal_range
            MinFrequency = 80;
            MaxFrequency = 1100;

            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(MinFrequency, MaxFrequency, FrequencyBands);
        }
    }
}
