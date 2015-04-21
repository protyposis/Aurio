using AudioAlign.Audio.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    public class FingerprintStore {

        private Dictionary<AudioTrack, TrackStore> store;
        private IFingerprintCollisionMap collisionMap;

        public FingerprintStore() {
            store = new Dictionary<AudioTrack, TrackStore>();
            collisionMap = new DictionaryCollisionMap();
        }

        public IFingerprintCollisionMap CollisionMap {
            get { return collisionMap; }
        }

        public void Add(FingerprintHashEventArgs e) {
            if (e.Hashes.Count == 0) {
                return;
            }

            lock (this) {
                // Make sure there's an index list for the track
                if (!store.ContainsKey(e.AudioTrack)) {
                    store.Add(e.AudioTrack, new TrackStore());
                }
                // Add the current length of the hash list as start pointer for all hashes belonging to the current index
                store[e.AudioTrack].index.Add(e.Index, new TrackStore.IndexEntry(store[e.AudioTrack].hashes.Count, e.Hashes.Count));

                foreach (var hash in e.Hashes) {
                    store[e.AudioTrack].hashes.Add(hash);
                    // insert a track/index lookup entry for the fingerprint hash
                    collisionMap.Add(hash, new FingerprintHashLookupEntry(e.AudioTrack, e.Index));
                }
            }
        }

        public List<Match> FindMatches(FingerprintHash hash) {
            List<Match> matches = new List<Match>();
            List<FingerprintHashLookupEntry> entries = collisionMap.GetValues(hash);

            for (int x = 0; x < entries.Count; x++) {
                FingerprintHashLookupEntry entry1 = entries[x];
                for (int y = x; y < entries.Count; y++) {
                    FingerprintHashLookupEntry entry2 = entries[y];
                    if (entry1.AudioTrack != entry2.AudioTrack) { // don't compare tracks with themselves

                        var store1 = store[entry1.AudioTrack];
                        var store2 = store[entry2.AudioTrack];
                        List<FingerprintHash> hashes1 = store1.hashes;
                        List<FingerprintHash> hashes2 = store2.hashes;
                        int index1 = entry1.Index;
                        int index2 = entry2.Index;
                        TrackStore.IndexEntry indexEntry1, indexEntry2;
                        int numTried = 0;
                        int numMatched = 0;

                        while(store1.index.ContainsKey(index1) && store2.index.ContainsKey(index2) && numTried < 200) {
                            indexEntry1 = store1.index[index1];
                            indexEntry2 = store2.index[index2];

                            int ii = 0;
                            for (int i = indexEntry1.index; i < indexEntry1.EndIndex; i++) {
                                for (int j = indexEntry2.index + ii; j < indexEntry2.EndIndex; j++) {
                                    if (hashes1[i] == hashes2[j]) {
                                        numMatched++;
                                    }
                                    else {
                                        numTried++;
                                    }
                                }
                                ii++;
                            }
                            index1++;
                            index2++;
                        }

                        if (numMatched > 50 && numTried > 100) {
                            matches.Add(new Match {
                                Similarity = 1 / numTried * numMatched,
                                Track1 = entry1.AudioTrack,
                                Track1Time = FingerprintGenerator.FingerprintHashIndexToTimeSpan(entry1.Index),
                                Track2 = entry2.AudioTrack,
                                Track2Time = FingerprintGenerator.FingerprintHashIndexToTimeSpan(entry2.Index),
                                Source = "FP-W03"
                            });
                        }
                    }
                }
            }

            return matches;
        }

        public List<Match> FindAllMatches() {
            List<Match> matches = new List<Match>();

            var collidingKeys = collisionMap.GetCollidingKeys();
            Debug.WriteLine("{0} colliding keys", collidingKeys.Count);

            foreach (FingerprintHash hash in collidingKeys) {
                matches.AddRange(FindMatches(hash));
            }
            Debug.WriteLine("{0} matches", matches.Count);

            return matches;
        }

        private class TrackStore {

            [DebuggerDisplay("{index}/{length}")]
            public struct IndexEntry {
                
                public int index;
                public int length;

                public IndexEntry(int index, int length) {
                    this.index = index;
                    this.length = length;
                }

                public int EndIndex {
                    get { return index + length; }
                }
            }

            public List<FingerprintHash> hashes;
            public Dictionary<int, IndexEntry> index;

            public TrackStore() {
                hashes = new List<FingerprintHash>();
                index = new Dictionary<int, IndexEntry>();
            }
        }
    }
}
