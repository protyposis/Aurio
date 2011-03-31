using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Collections.Specialized;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class SubFingerprintEventArgs : EventArgs {

        public SubFingerprintEventArgs(AudioTrack audioTrack, SubFingerprint subFingerprint, TimeSpan timestamp, bool variation) {
            AudioTrack = audioTrack;
            SubFingerprint = subFingerprint;
            Timestamp = timestamp;
            IsVariation = variation;
        }

        public AudioTrack AudioTrack { get; private set; }
        public SubFingerprint SubFingerprint { get; private set; }
        public TimeSpan Timestamp { get; private set; }
        /// <summary>
        /// Gets a value telling if the subfingerprint is a variation of an original fingerprint (which means
        /// the original subfingerprint has been modified) or an unmodified original fingerprint.
        /// </summary>
        public bool IsVariation { get; private set; }
    }
}
