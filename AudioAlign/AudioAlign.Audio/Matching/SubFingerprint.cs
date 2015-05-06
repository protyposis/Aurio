using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching {
    [DebuggerDisplay("{Index}/{Hash}")]
    public struct SubFingerprint {

        public int Index;
        public SubFingerprintHash Hash;
        /// <summary>
        /// Gets a value telling if the subfingerprint is a variation of an original fingerprint (which means
        /// the original subfingerprint has been modified) or an unmodified original fingerprint.
        /// </summary>
        public bool IsVariation;

        public SubFingerprint(int index, SubFingerprintHash hash, bool variation) {
            Index = index;
            Hash = hash;
            IsVariation = variation;
        }
    }
}
