using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    class BassProfile : DefaultProfile {

        // bass range 80 - 350 Hz: http://www.listenhear.co.uk/general_acoustics.htm
        protected new const int FREQ_MIN = 80;
        protected new const int FREQ_MAX = 350;

        public BassProfile() {
            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(FREQ_MIN, FREQ_MAX, FREQ_BANDS);
        }

        public override string Name { 
            get { return "Bass"; } 
        }
    }
}
