using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    [DebuggerDisplay("{Frame}/{Hash}")]
    public struct FPHash {

        public uint Frame;
        public uint Hash;

        public FPHash(uint frame, uint hash) {
            Frame = frame;
            Hash = hash;
        }
    }
}
