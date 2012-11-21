using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Collections.Specialized;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class SubFingerprintEventArgs : EventArgs {

        public SubFingerprintEventArgs(AudioTrack audioTrack, SubFingerprint subFingerprint, int index, int indices, bool variation) {
            AudioTrack = audioTrack;
            SubFingerprint = subFingerprint;
            Index = index;
            Indices = indices;
            IsVariation = variation;
        }

        public AudioTrack AudioTrack { get; private set; }
        public SubFingerprint SubFingerprint { get; private set; }
        public int Index { get; private set; }
        public int Indices { get; private set; }
        /// <summary>
        /// Gets a value telling if the subfingerprint is a variation of an original fingerprint (which means
        /// the original subfingerprint has been modified) or an unmodified original fingerprint.
        /// </summary>
        public bool IsVariation { get; private set; }
    }
}
