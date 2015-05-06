using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    public class SyncProfile : ChromaprintProfile {
        public SyncProfile() : base() {
            Name = "Sync optimized";

            // The same settings as in the Haitsma&Kalker default profile
            SamplingRate = 5512;
            WindowSize = 2048;
            HopSize = 64;

            ChromaMappingMode = ChromaMappingMode.Paper;
        }
    }
}
