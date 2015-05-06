using AudioAlign.Audio.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    public class FingerprintStore {

        private Profile profile;
        private Dictionary<AudioTrack, TrackStore> store;
        private IFingerprintCollisionMap collisionMap;

        private double[] thresholdAccept;
        private double[] thresholdReject;

        public event EventHandler<ValueEventArgs<double>> MatchingProgress;
        public event EventHandler<ValueEventArgs<List<Match>>> MatchingFinished;

        public FingerprintStore(Profile profile) {
            this.profile = profile;
            store = new Dictionary<AudioTrack, TrackStore>();
            collisionMap = new DictionaryCollisionMap();

            // Precompute the threshold function
            thresholdAccept = new double[profile.MatchingMaxFrames];
            thresholdReject = new double[profile.MatchingMaxFrames];
            double framesPerSec = (double)profile.SamplingRate / profile.HopSize;
            for (int i = 0; i < thresholdAccept.Length; i++) {
                thresholdAccept[i] = profile.ThresholdAccept.Calculate(i / framesPerSec);
                thresholdReject[i] = profile.ThresholdReject.Calculate(i / framesPerSec);
            }
        }

        public IFingerprintCollisionMap CollisionMap {
            get { return collisionMap; }
        }

        public void Add(SubFingerprintsGeneratedEventArgs e) {
            if (e.SubFingerprints.Count == 0) {
                return;
            }

            lock (this) {
                // Make sure there's an index list for the track
                if (!store.ContainsKey(e.AudioTrack)) {
                    store.Add(e.AudioTrack, new TrackStore());
                }
                // Add the current length of the hash list as start pointer for all hashes belonging to the current index
                store[e.AudioTrack].index.Add(e.Index, new TrackStore.IndexEntry(store[e.AudioTrack].hashes.Count, e.SubFingerprints.Count));

                foreach (var hash in e.SubFingerprints) {
                    store[e.AudioTrack].hashes.Add(hash.Hash);
                    // insert a track/index lookup entry for the fingerprint hash
                    collisionMap.Add(hash.Hash, new SubFingerprintLookupEntry(e.AudioTrack, hash.Index));
                }
            }
        }

        public List<Match> FindMatches(SubFingerprintHash hash) {
            List<Match> matches = new List<Match>();
            List<SubFingerprintLookupEntry> entries = collisionMap.GetValues(hash);

            for (int x = 0; x < entries.Count; x++) {
                SubFingerprintLookupEntry entry1 = entries[x];
                for (int y = x; y < entries.Count; y++) {
                    SubFingerprintLookupEntry entry2 = entries[y];
                    if (entry1.AudioTrack != entry2.AudioTrack) { // don't compare tracks with themselves

                        var store1 = store[entry1.AudioTrack];
                        var store2 = store[entry2.AudioTrack];
                        List<SubFingerprintHash> hashes1 = store1.hashes;
                        List<SubFingerprintHash> hashes2 = store2.hashes;
                        int index1 = entry1.Index;
                        int index2 = entry2.Index;
                        TrackStore.IndexEntry indexEntry1, indexEntry2;
                        int numTried = 0; // count of hashes tried to match
                        int numMatched = 0; // count of hashes matched
                        int frameCount = 0; // count over how many actual frames hashes were matched (an index in the store is the equivalent of a frame in the generator)
                        bool matchFound = false;

                        // Iterate through sequential frames
                        while(store1.index.ContainsKey(index1) && store2.index.ContainsKey(index2)) {
                            indexEntry1 = store1.index[index1];
                            indexEntry2 = store2.index[index2];

                            // Hash collision
                            // The union of the two ranges is the total number of distinct hashes
                            // The intersection of the two ranges is the total number of similar hashes
                            // NOTE The following block calculates the number of matches (the intersection)
                            //      of the two hash lists with the Zipper intersection algorithm and relies
                            //      on the hash list sorting in the fingerprint generator.
                            //      Other approaches tried which are slower:
                            //      - n*m element by element comparison (seven though the amount of elements is reasonably small)
                            //      - concatenating the two ranges (LINQ), sorting them, and linearly iterating over, counting the duplicates (sort is slow)
                            //      - using a hash set for collision detection (hash set insertion and lookup are costly)

                            int i = indexEntry1.index;
                            int i_e = indexEntry1.index + indexEntry1.length;
                            int j = indexEntry2.index;
                            int j_e = indexEntry2.index + indexEntry2.length;
                            int intersectionCount = 0;

                            // Count intersecting hashes of a frame with the Zipper algorithm
                            while (i < i_e && j < j_e) {
                                if (hashes1[i] < hashes2[j]) {
                                    i++;
                                }
                                else if (hashes2[j] < hashes1[i]) {
                                    j++;
                                }
                                else {
                                    intersectionCount++;
                                    i++; j++;
                                }
                            }

                            numMatched += intersectionCount;
                            numTried += indexEntry1.length + indexEntry2.length - intersectionCount;

                            index1++;
                            index2++;
                            frameCount++;

                            // Match detection
                            // This approach trades the hash matching rate with time, i.e. the rate required
                            // for a match drops with time, by using an exponentially with time decaying threshold. 
                            // The idea is that a high matching rate after a short time is equivalent to a low matching 
                            // rate after a long time. The difficulty is to to parameterize it in such a way, that a 
                            // match is detected as fast as possible, while detecting a no-match isn't delayed too far 
                            // as it takes a lot of processing time.
                            // NOTE The current parameters are just eyeballed, there's a lot of influence on processing speed here
                            //double threshold = Math.Pow(profile.MatchingThresholdExponentialDecayBase, numIndices / sec / profile.MatchingThresholdExponentialWidthScale) * profile.MatchingThresholdExponentialHeight; // match successful threshold
                            //double thresholdLow = threshold / profile.MatchingThresholdRejectionFraction; // match abort threshold
                            double rate = 1d / numTried * numMatched;

                            if (frameCount > profile.MatchingMinFrames && rate > thresholdAccept[frameCount]) {
                                matchFound = true;
                                break;
                            }
                            else if (rate < thresholdReject[frameCount] || frameCount > profile.MatchingMaxFrames) {
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
            
            int collisions = collidingKeys.Count;
            int count = 0;
            foreach (SubFingerprintHash hash in collidingKeys) {
                matches.AddRange(FindMatches(hash));

                if (count++ % 4096 == 0 && MatchingProgress != null) {
                    MatchingProgress(this, new ValueEventArgs<double>((double)count / collisions * 100));
                }
            }
            Debug.WriteLine("{0} matches", matches.Count);

            if (MatchingFinished != null) {
                MatchingFinished(this, new ValueEventArgs<List<Match>>(matches));
            }
            
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
            }

            public List<SubFingerprintHash> hashes;
            public Dictionary<int, IndexEntry> index;

            public TrackStore() {
                hashes = new List<SubFingerprintHash>();
                index = new Dictionary<int, IndexEntry>();
            }
        }
    }
}
