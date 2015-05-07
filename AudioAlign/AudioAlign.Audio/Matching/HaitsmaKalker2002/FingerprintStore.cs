using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Diagnostics;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class FingerprintStore {

        public const int DEFAULT_FINGERPRINT_SIZE = 256;
        public const float DEFAULT_THRESHOLD = 0.35f;

        private int fingerprintSize;
        private float threshold;
        private Profile profile;
        private Dictionary<AudioTrack, List<SubFingerprintHash>> store;
        private IFingerprintCollisionMap collisionMap;

        public FingerprintStore(Profile profile) {
            FingerprintSize = DEFAULT_FINGERPRINT_SIZE;
            Threshold = DEFAULT_THRESHOLD;
            this.profile = profile;
            store = new Dictionary<AudioTrack, List<SubFingerprintHash>>();

            // Dictionary is faster, SQLite needs less memory
            collisionMap = new DictionaryCollisionMap(); // new SQLiteCollisionMap();

            /*
             * TODO to support processing of huge datasets (or machines with low memory),
             * the store could also be moved from Dictionary/Lists to SQLite, the database
             * written to disk (instead of in-memory like now) and the user given the 
             * choice between them (or automatically chosen depending on the amount of data).
             */
        }

        public int FingerprintSize {
            get { return fingerprintSize; }
            set {
                if (value < 1) {
                    throw new ArgumentOutOfRangeException("the fingerprint size must be at least 1");
                }
                fingerprintSize = value;
            }
        }

        public float Threshold {
            get { return threshold; }
            set {
                if (value < 0.0f || value > 1.0f) {
                    throw new ArgumentOutOfRangeException("the threshold must be between 0 and 1");
                }
                threshold = value;
            }
        }

        public IFingerprintCollisionMap CollisionMap {
            get { return collisionMap; }
        }

        public Dictionary<AudioTrack, List<SubFingerprintHash>> AudioTracks {
            get { return store; }
        }

        public void Add(SubFingerprintsGeneratedEventArgs e) {
            if (e.SubFingerprints.Count == 0) {
                return;
            }

            lock (this) {
                if (!store.ContainsKey(e.AudioTrack)) {
                    store.Add(e.AudioTrack, new List<SubFingerprintHash>());
                }

                foreach (var sfp in e.SubFingerprints) {
                    if (!sfp.IsVariation) {
                        // store the sub-fingerprint in the sequential list of the audio track
                        store[e.AudioTrack].Add(sfp.Hash);
                    }

                    // insert a track/index lookup entry for the sub-fingerprint
                    collisionMap.Add(sfp.Hash, new SubFingerprintLookupEntry(e.AudioTrack, sfp.Index));
                }
            }
        }

        public List<Match> FindMatches(SubFingerprintHash hash) {
            List<Match> matches = new List<Match>();
            List<SubFingerprintLookupEntry> entries = collisionMap.GetValues(hash);

            //Debug.WriteLine("Finding matches...");

            // compare each track with each other
            int cycle = 1;
            for (int x = 0; x < entries.Count; x++) {
                SubFingerprintLookupEntry entry1 = entries[x];
                for (int y = cycle; y < entries.Count; y++) {
                    SubFingerprintLookupEntry entry2 = entries[y];
                    if (entry1.AudioTrack != entry2.AudioTrack) { // don't compare tracks with themselves
                        //Debug.WriteLine("Comparing " + entry1.AudioTrack.Name + " with " + entry2.AudioTrack.Name + ":");
                        if (store[entry1.AudioTrack].Count - entry1.Index < fingerprintSize
                            || store[entry2.AudioTrack].Count - entry2.Index < fingerprintSize) {
                            // the end of at least one track has been reached and there are not enough hashes left
                            // to do a fingerprint comparison
                            continue;
                        }

                        // sum up the bit errors
                        List<SubFingerprintHash> track1Hashes = store[entry1.AudioTrack];
                        List<SubFingerprintHash> track2Hashes = store[entry2.AudioTrack];
                        uint bitErrors = 0;
                        for (int s = 0; s < fingerprintSize; s++) {
                            SubFingerprintHash track1Hash = track1Hashes[entry1.Index + s];
                            SubFingerprintHash track2Hash = track2Hashes[entry2.Index + s];
                            if (track1Hash.Value == 0 || track2Hash.Value == 0) {
                                bitErrors = (uint)fingerprintSize * 32;
                                break;
                            }
                            // skip fingerprints with hashes that are zero, since it is probably from 
                            // a track section with silence
                            // by setting the bitErrors to the maximum, the match will not be added
                            bitErrors += track1Hash.HammingDistance(track2Hash);
                        }

                        float bitErrorRate = bitErrors / (float)(fingerprintSize * 32); // sub-fingerprints * 32 bits
                        //Debug.WriteLine("BER: " + bitErrorRate + " <- " + (bitErrorRate < threshold ? "MATCH!!!" : "no match"));
                        if (bitErrorRate < threshold) {
                            matches.Add(new Match {
                                Similarity = 1 - bitErrorRate,
                                Track1 = entry1.AudioTrack,
                                Track1Time = SubFingerprintIndexToTimeSpan(entry1.Index),
                                Track2 = entry2.AudioTrack,
                                Track2Time = SubFingerprintIndexToTimeSpan(entry2.Index),
                                Source = "FP-HK02"
                            });
                        }
                    }
                }
                cycle++;
            }

            //Debug.WriteLine("finished");
            return matches;
        }

        public List<Match> FindAllMatches() {
            List<Match> matches = new List<Match>();
            //collisionMap.CreateLookupIndex(); // TODO evaluate if this call speeds the process up
            //collisionMap.Cleanup();
            foreach (SubFingerprintHash hash in collisionMap.GetCollidingKeys()) {
                // skip all hashes whose bits are all zero, since this is probably a position with silence
                if (hash.Value != 0) {
                    matches.AddRange(FindMatches(hash));
                }
            }
            return matches;
        }

        public Fingerprint GetFingerprint(SubFingerprintLookupEntry entry) {
            int indexOffset = 0;
            if (store[entry.AudioTrack].Count - entry.Index < fingerprintSize) {
                indexOffset = Math.Min(indexOffset, -fingerprintSize + store[entry.AudioTrack].Count - entry.Index);
            }
            return new Fingerprint(store[entry.AudioTrack], entry.Index + indexOffset, fingerprintSize);
        }

        private TimeSpan SubFingerprintIndexToTimeSpan(int index) {
            return new TimeSpan((long)Math.Round(index * profile.HashTimeScale * TimeUtil.SECS_TO_TICKS));
        }
    }
}
