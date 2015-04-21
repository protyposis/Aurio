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

        public override bool Equals(object obj) {
            return this.value.Equals(((FingerprintHash)obj).value);
        }

        public override int GetHashCode() {
            return this.value.GetHashCode();
        }

        public override string ToString() {
            return "FingerprintHash {" + Convert.ToString(this.value, 2).PadLeft(32, '0') + " (" + this.value + ")}";
        }

        public static bool operator !=(FingerprintHash a, FingerprintHash b) {
            return a.value != b.value;
        }

        public static bool operator ==(FingerprintHash a, FingerprintHash b) {
            return a.value == b.value;
        }
    }
}
