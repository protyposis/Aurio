using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class SubFingerprintLookupEntry {

        public SubFingerprintLookupEntry(AudioTrack audioTrack, int index) {
            AudioTrack = audioTrack;
            Index = index;
        }

        public SubFingerprintLookupEntry(AudioTrack audioTrack, int index, TimeSpan timestamp)
            : this(audioTrack, index) {
            Timestamp = timestamp;
        }

        // NOTE fields instead of properties to speed up fingerprint search (getters took ~20% cpu time)
        public AudioTrack AudioTrack;
        public int Index;
        public TimeSpan Timestamp;

        public override string ToString() {
            return "SubFingerprintLookupEntry {" + AudioTrack.GetHashCode() + " / " + Index + " / " + Timestamp + "}";
        }
    }
}
