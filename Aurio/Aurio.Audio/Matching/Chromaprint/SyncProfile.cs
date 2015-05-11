using Aurio.Audio.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Matching.Chromaprint {
    class SyncProfile : DefaultProfile {
        public SyncProfile() : base() {
            Name = "Sync optimized";

            // The same settings as in the Haitsma&Kalker default profile
            SamplingRate = 5512;
            WindowSize = 2048;
            HopSize = 64;

            ChromaMappingMode = Chroma.MappingMode.Paper;

            HashTimeScale = 1d / SamplingRate * HopSize;
        }
    }
}
