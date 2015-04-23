using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Features.ContinuousFrequencyActivation {
    class ContinuousFrequencyActivationQuantifier : StrongPeakDetector {

        private float[] strongPeakValues; // buffer

        public ContinuousFrequencyActivationQuantifier(IAudioStream stream)
            : base(stream) {
                strongPeakValues = new float[WindowSize / 2];
        }

        public override void ReadFrame(float[] cfa) {
            base.ReadFrame(strongPeakValues);

            int peakCount = (int)strongPeakValues[strongPeakValues.Length - 1];
            Array.Sort<float>(strongPeakValues, 0, peakCount); // sort...
            Array.Reverse(strongPeakValues, 0, peakCount); // ...descending

            // sum largest peak values to characterize overall "peakiness"
            cfa[0] = 0;
            for (int i = 0; i < 5; i++) {
                cfa[0] += strongPeakValues[i];
            }
        }
    }
}
