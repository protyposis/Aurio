using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    class BassProfile : DefaultProfile {

        public BassProfile() : base() {
            Name = "Bass";

            // bass range 80 - 350 Hz: http://www.listenhear.co.uk/general_acoustics.htm
            MinFrequency = 80;
            MaxFrequency = 350;

            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(MinFrequency, MaxFrequency, FrequencyBands);
        }
    }
}
