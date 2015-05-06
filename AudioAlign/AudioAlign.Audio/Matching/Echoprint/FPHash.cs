using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    [DebuggerDisplay("{Frame}/{Hash}")]
    public struct FPHash {

        public uint Frame;
        public SubFingerprint Hash;

        public FPHash(uint frame, SubFingerprint hash) {
            Frame = frame;
            Hash = hash;
        }
    }
}
