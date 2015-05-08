using AudioAlign.Audio.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    public class FingerprintStore : Wang2003.FingerprintStore {

        public FingerprintStore(Profile profile) {
            // Precompute the threshold function
            var thresholdAccept = new double[profile.MatchingMaxFrames];
            var thresholdReject = new double[profile.MatchingMaxFrames];
            for (int i = 0; i < thresholdAccept.Length; i++) {
                thresholdAccept[i] = profile.ThresholdAccept.Calculate(i * profile.HashTimeScale);
                thresholdReject[i] = profile.ThresholdReject.Calculate(i * profile.HashTimeScale);
            }

            Initialize(profile, profile.MatchingMinFrames, profile.MatchingMaxFrames, thresholdAccept, thresholdReject, "FP-EP");
        }
    }
}
