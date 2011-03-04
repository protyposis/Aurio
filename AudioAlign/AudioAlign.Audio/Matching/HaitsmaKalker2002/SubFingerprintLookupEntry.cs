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

        public AudioTrack AudioTrack { get; private set; }
        public int Index { get; private set; }
        public TimeSpan Timestamp { get; private set; }

        public override string ToString() {
            return "SubFingerprintLookupEntry {" + AudioTrack.GetHashCode() + " / " + Index + " / " + Timestamp + "}";
        }
    }
}
