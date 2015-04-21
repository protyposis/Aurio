using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    public struct FingerprintHash {

        private UInt32 value;

        public FingerprintHash(UInt32 value) {
            this.value = value;
        }

        public UInt32 Value { 
            get { return this.value; } 
        }
    }
}
