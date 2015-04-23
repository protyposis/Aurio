using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;

namespace AudioAlign.Audio.Matching.Wang2003 {

    /// <summary>
    /// Copied from <see cref="AudioAlign.Audio.Matching.HaitsmaKalker2002.SubFingerprintLookupEntry"/>
    /// </summary>
    public struct FingerprintHashLookupEntry {

        public FingerprintHashLookupEntry(AudioTrack audioTrack, int index) {
            AudioTrack = audioTrack;
            Index = index;
        }

        public AudioTrack AudioTrack;
        public int Index;

        public override string ToString() {
            return "FingerprintHashLookupEntry {" + AudioTrack.GetHashCode() + " / " + Index + "}";
        }
    }
}
