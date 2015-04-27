using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    [DebuggerDisplay("{Frame}/{Code}")]
    struct FPCode {

        private uint Frame;
        private uint Code;

        public FPCode(uint frame, uint code) {
            Frame = frame;
            Code = code;
        }
    }
}
