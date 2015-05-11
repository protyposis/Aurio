using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    class HumanProfile : DefaultProfile {

        public HumanProfile() : base() {
            Name = "Human (slow!)";

            FrameSize = 16384;
            FrameStep = 512;
            SampleRate = 44100;

            MinFrequency = 80;
            MaxFrequency = 16000;
            FrequencyBands = 33;

            HashTimeScale = 1d / SampleRate * FrameStep;

            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(MinFrequency, MaxFrequency, FrequencyBands);
        }
    }
}
