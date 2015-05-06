using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching {
    [DebuggerDisplay("{Frame}/{Hash}")]
    public struct IndexedSubFingerprint {

        public int Index;
        public SubFingerprintHash SubFingerprint;
        /// <summary>
        /// Gets a value telling if the subfingerprint is a variation of an original fingerprint (which means
        /// the original subfingerprint has been modified) or an unmodified original fingerprint.
        /// </summary>
        public bool IsVariation;

        public IndexedSubFingerprint(int index, SubFingerprintHash subFingerprint, bool variation) {
            Index = index;
            SubFingerprint = subFingerprint;
            IsVariation = variation;
        }
    }
}
