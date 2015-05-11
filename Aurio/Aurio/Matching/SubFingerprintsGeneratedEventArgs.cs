using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Project;
using System.Collections.Specialized;

namespace Aurio.Matching {
    public class SubFingerprintsGeneratedEventArgs : EventArgs {

        public SubFingerprintsGeneratedEventArgs(AudioTrack audioTrack, List<SubFingerprint> subFingerprints, int index, int indices) {
            AudioTrack = audioTrack;
            SubFingerprints = subFingerprints;
            Index = index;
            Indices = indices;
        }

        public SubFingerprintsGeneratedEventArgs(AudioTrack audioTrack, SubFingerprint subFingerprint, int index, int indices)
            : this(audioTrack, new List<SubFingerprint>(new SubFingerprint[] { subFingerprint }), index, indices) { }

        public AudioTrack AudioTrack { get; private set; }
        public List<SubFingerprint> SubFingerprints { get; private set; }
        public int Index { get; private set; }
        public int Indices { get; private set; }
    }
}
