using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Diagnostics;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class FingerprintStore {

        public class SubFingerprintLookupEntry {
            public AudioTrack AudioTrack { get; set; }
            public int Index { get; set; }

            public override string ToString() {
                return "SubFingerprintLookupEntry {" + AudioTrack.GetHashCode() + " / " + Index + "}";
            }
        }

        private Dictionary<SubFingerprint, List<SubFingerprintLookupEntry>> lookupTable;
        private Dictionary<AudioTrack, List<SubFingerprint>> store;

        public FingerprintStore() {
            lookupTable = new Dictionary<SubFingerprint, List<SubFingerprintLookupEntry>>();
            store = new Dictionary<AudioTrack, List<SubFingerprint>>();
        }

        public Dictionary<SubFingerprint, List<SubFingerprintLookupEntry>> LookupTable {
            get { return lookupTable; }
        }

        public Dictionary<AudioTrack, List<SubFingerprint>> AudioTracks {
            get { return store; }
        }

        public void Add(AudioTrack audioTrack, SubFingerprint subFingerprint) {
            // store the sub-fingerprint in the sequential list of the audio track
            if (!store.ContainsKey(audioTrack)) {
                store.Add(audioTrack, new List<SubFingerprint>());
            }
            store[audioTrack].Add(subFingerprint);

            // insert a track/index lookup entry for the sub-fingerprint
            if (!lookupTable.ContainsKey(subFingerprint)) {
                lookupTable.Add(subFingerprint, new List<SubFingerprintLookupEntry>());
            }
            lookupTable[subFingerprint].Add(new SubFingerprintLookupEntry {
                AudioTrack = audioTrack,
                Index = store[audioTrack].Count
            });
        }

        public void Analyze() {
            Debug.WriteLine("analyzing fingerprint store...");
            foreach (SubFingerprint sfp in lookupTable.Keys) {
                if (lookupTable[sfp].Count > 1) {
                    Debug.WriteLine(sfp + " has " + lookupTable[sfp].Count + " matches:");
                    foreach (SubFingerprintLookupEntry le in lookupTable[sfp]) {
                        Debug.WriteLine(le.AudioTrack + ": " + le.Index);
                    }
                }
            }
            Debug.WriteLine("analysis finished");
        }
    }
}
