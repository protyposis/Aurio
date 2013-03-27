using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {

    /// <summary>
    /// Struct consumes a little bit less memory than object (~5%) and results in a faster fingerprint
    /// generation time (~9%).
    /// 
    /// 
    /// </summary>
    public struct SubFingerprintLookupEntry {

        public SubFingerprintLookupEntry(AudioTrack audioTrack, int index) {
            AudioTrack = audioTrack;
            Index = index;
        }

        // NOTE fields instead of properties to speed up fingerprint search (getters took ~20% cpu time)
        public AudioTrack AudioTrack;
        public int Index;

        public override string ToString() {
            return "SubFingerprintLookupEntry {" + AudioTrack.GetHashCode() + " / " + Index + "}";
        }
    }
}
