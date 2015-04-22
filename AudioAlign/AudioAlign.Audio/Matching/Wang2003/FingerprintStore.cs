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
            Dictionary<FingerprintHash, int> localCollisionMap = new Dictionary<FingerprintHash, int>();

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
                        int numTried = 0; // count of hashes tried to match
                        int numMatched = 0; // count of hashes matched
                        int numIndices = 0; // count over how many actual frames hashes were matched (an index in the store is the equivalent of a frame in the generator)
                        bool matchFound = false;

                        while(store1.index.ContainsKey(index1) && store2.index.ContainsKey(index2)) {
                            indexEntry1 = store1.index[index1];
                            indexEntry2 = store2.index[index2];

                            // Determine union and intersection (this block is much faster than using LINQ)
                            // The union of the two ranges is the total number of distinct hashes
                            // The intersection of the two ranges is the total number of similar hashes
                            localCollisionMap.Clear();
                            for (int i = indexEntry1.index; i < indexEntry1.EndIndex; i++) {
                                localCollisionMap.Add(hashes1[i], 0);
                                numTried++;
                            }
                            for (int j = indexEntry2.index; j < indexEntry2.EndIndex; j++) {
                                if (localCollisionMap.ContainsKey(hashes2[j])) {
                                    numMatched++; // if it's already contained in the map, it's a matching hash
                                }
                                else {
                                    numTried++; // if it's not contained, it's another distinct hash
                                }
                            }

                            index1++;
                            index2++;
                            numIndices++;

                            // Match detection
                            // This approach trades the hash matching rate with time, i.e. the rate required
                            // for a match drops with time. The idea is that a high matching rate after a short
                            // time is equivalent to a low matching rate after a long time. The difficulty is to 
                            // to parameterize it in such a way, that a match is detected as fast as possible,
                            // while detecting a no-match isn't delayed too far as it takes a lot of processing time.
                            // NOTE The current parameters are just eyeballed, there's a lot of influence on processing speed here
                            double sec = 11025d / 256;
                            double threshold = Math.Pow(0.5, numIndices / (sec * 2)) * 0.3;
                            double thresholdLow = threshold / 6;
                            double rate = 1d / numTried * numMatched;

                            if (numIndices > 10 && rate > threshold) {
                                matchFound = true;
                                break;
                            }
                            else if (rate < thresholdLow || numIndices > 30 * sec) {
                                break; // exit condition
                            }
                        }

                        if (matchFound) {
                            matches.Add(new Match {
                                Similarity = 1f / numTried * numMatched,
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
            Debug.WriteLine("{0} colliding keys, {1} lookup entries", collidingKeys.Count, collidingKeys.Sum(h => collisionMap.GetValues(h).Count));

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
