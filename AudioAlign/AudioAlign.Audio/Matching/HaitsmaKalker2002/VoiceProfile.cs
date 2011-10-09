using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    class VoiceProfile : DefaultProfile {

        // vocal range 80 - 1100 Hz: http://en.wikipedia.org/wiki/Vocal_range
        protected new const int FREQ_MIN = 80;
        protected new const int FREQ_MAX = 1100;

        public VoiceProfile() {
            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(FREQ_MIN, FREQ_MAX, FREQ_BANDS);
        }

        public override string Name { 
            get { return "Voice"; } 
        }
    }
}
