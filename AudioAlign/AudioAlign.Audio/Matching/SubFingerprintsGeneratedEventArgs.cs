using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Collections.Specialized;

namespace AudioAlign.Audio.Matching {
    public class SubFingerprintsGeneratedEventArgs : EventArgs {

        public SubFingerprintsGeneratedEventArgs(AudioTrack audioTrack, List<IndexedSubFingerprint> subFingerprints, int index, int indices) {
            AudioTrack = audioTrack;
            SubFingerprints = subFingerprints;
            Index = index;
            Indices = indices;
        }

        public SubFingerprintsGeneratedEventArgs(AudioTrack audioTrack, IndexedSubFingerprint subFingerprint, int index, int indices)
            : this(audioTrack, new List<IndexedSubFingerprint>(new IndexedSubFingerprint[] { subFingerprint }), index, indices) { }

        public AudioTrack AudioTrack { get; private set; }
        public List<IndexedSubFingerprint> SubFingerprints { get; private set; }
        public int Index { get; private set; }
        public int Indices { get; private set; }
    }
}
