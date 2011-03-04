using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Collections.Specialized;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class SubFingerprintEventArgs : EventArgs {

        public SubFingerprintEventArgs(AudioTrack audioTrack, SubFingerprint subFingerprint, TimeSpan timestamp) {
            AudioTrack = audioTrack;
            SubFingerprint = subFingerprint;
            Timestamp = timestamp;
        }

        public AudioTrack AudioTrack { get; private set; }
        public SubFingerprint SubFingerprint { get; private set; }
        public TimeSpan Timestamp { get; private set; }
    }
}
