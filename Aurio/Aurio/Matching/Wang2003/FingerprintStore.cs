// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using Aurio.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Wang2003 {
    public class FingerprintStore {

        private IProfile profile;
        private Dictionary<AudioTrack, TrackStore> store;
        private IFingerprintCollisionMap collisionMap;

        private int matchingMinFrames;
        private int matchingMaxFrames;
        private double[] thresholdAccept;
        private double[] thresholdReject;

        private string matchSourceName;

        public event EventHandler<ValueEventArgs<double>> MatchingProgress;
        public event EventHandler<ValueEventArgs<List<Match>>> MatchingFinished;

        protected FingerprintStore() {
            // called by subclasses
        }

        public FingerprintStore(Profile profile) {
            // Precompute the threshold function
            var thresholdAccept = new double[profile.MatchingMaxFrames];
            var thresholdReject = new double[profile.MatchingMaxFrames];
            for (int i = 0; i < thresholdAccept.Length; i++) {
                thresholdAccept[i] = profile.ThresholdAccept.Calculate(i * profile.HashTimeScale);
                thresholdReject[i] = profile.ThresholdReject.Calculate(i * profile.HashTimeScale);
            }

            // Setup the store
            Initialize(profile, profile.MatchingMinFrames, profile.MatchingMaxFrames, thresholdAccept, thresholdReject, "FP-W03");
        }

        protected void Initialize(IProfile profile, int matchingMinFrames, int matchingMaxFrames, double[] thresholdAccept, double[] thresholdReject, string matchSourceName) {
            this.profile = profile;
            this.matchingMinFrames = matchingMinFrames;
            this.matchingMaxFrames = matchingMaxFrames;
            this.thresholdAccept = thresholdAccept;
            this.thresholdReject = thresholdReject;
            this.matchSourceName = matchSourceName;

            store = new Dictionary<AudioTrack, TrackStore>();
            collisionMap = new DictionaryCollisionMap();
        }

        public IFingerprintCollisionMap CollisionMap {
            get { return collisionMap; }
        }

        public void Add(AudioTrack audioTrack, List<SubFingerprint> subFingerprints) {
            if (subFingerprints.Count == 0) {
                return;
            }

            lock (this) {
                // Make sure there's a store for the track and get it
                if (!store.ContainsKey(audioTrack)) {
                    store.Add(audioTrack, new TrackStore());
                }
                var trackStore = store[audioTrack];

                int hashListIndex = 0;
                SubFingerprint hash;

                // Iterate through the sequence of input hashes and add them to the store (in batches of the same frame index)
                while (subFingerprints.Count > hashListIndex) {
                    int storeHashIndex = trackStore.hashes.Count;
                    int storeIndex = subFingerprints[hashListIndex].Index;
                    int hashCount = 0;

                    // Count all sequential input hashes with the same frame index (i.e. batch) and add them to the store
                    while (subFingerprints.Count > hashListIndex + hashCount
                        && (hash = subFingerprints[hashListIndex + hashCount]).Index == storeIndex) {
                        // Insert hash into the sequential store
                        trackStore.hashes.Add(hash.Hash);

                        // Insert a track/index lookup entry for the fingerprint hash
                        collisionMap.Add(hash.Hash, new SubFingerprintLookupEntry(audioTrack, hash.Index));

                        hashCount++;
                    }

                    // Add an index entry which tells where a hash with a specific frame index can be found in the store
                    if (hashCount > 0) {
                        TrackStore.IndexEntry ie;
                        // If there is already an entry for the frame index, take it and update its length, ...
                        if (trackStore.index.ContainsKey(storeIndex)) {
                            ie = trackStore.index[storeIndex];
                            ie.length += hashCount;
                            trackStore.index.Remove(storeIndex);
                        }
                        else { // ... else create a new entry
                            ie = new TrackStore.IndexEntry(storeHashIndex, hashCount);
                        }
                        // Add the current length of the hash list as start pointer for all hashes belonging to the current index
                        trackStore.index.Add(storeIndex, ie);
                    }

                    hashListIndex += hashCount;
                }
            }
        }

        public void Add(SubFingerprintsGeneratedEventArgs e)
        {
            Add(e.AudioTrack, e.SubFingerprints);
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
                        TrackStore.IndexEntry indexEntryNone = new TrackStore.IndexEntry();
                        while (true) {
                            indexEntry1 = store1.index.ContainsKey(index1) ? store1.index[index1] : indexEntryNone;
                            indexEntry2 = store2.index.ContainsKey(index2) ? store2.index[index2] : indexEntryNone;

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

                            // Determine the next indices to check for collisions
                            int nextIndex1Increment = 0;
                            if (hashes1.Count > i_e) {
                                do {
                                    nextIndex1Increment++;
                                } while (!store1.index.ContainsKey(index1 + nextIndex1Increment));
                            }
                            int nextIndex2Increment = 0;
                            if (hashes2.Count > j_e) {
                                do {
                                    nextIndex2Increment++;
                                } while (!store2.index.ContainsKey(index2 + nextIndex2Increment));
                            }
                            int nextIndexIncrement = Math.Min(nextIndex1Increment, nextIndex2Increment);

                            index1 += nextIndexIncrement;
                            index2 += nextIndexIncrement;
                            frameCount += nextIndexIncrement;

                            // Match detection
                            // This approach trades the hash matching rate with time, i.e. the rate required
                            // for a match drops with time, by using an exponentially with time decaying threshold. 
                            // The idea is that a high matching rate after a short time is equivalent to a low matching 
                            // rate after a long time. The difficulty is to to parameterize it in such a way, that a 
                            // match is detected as fast as possible, while detecting a no-match isn't delayed too far 
                            // as it takes a lot of processing time.
                            // NOTE The current parameters are just eyeballed, there's a lot of influence on processing speed and matching rate here
                            double rate = 1d / numTried * numMatched;

                            if (frameCount >= matchingMaxFrames || rate < thresholdReject[frameCount]) {
                                break; // exit condition
                            }
                            else if (frameCount > matchingMinFrames && rate > thresholdAccept[frameCount]) {
                                matchFound = true;
                                break;
                            }

                            if (nextIndexIncrement == 0) {
                                // We reached the end of a hash list
                                break; // Break the while loop
                            }
                        }

                        if (matchFound) {
                            matches.Add(new Match {
                                Similarity = 1f / numTried * numMatched,
                                Track1 = entry1.AudioTrack,
                                Track1Time = SubFingerprintIndexToTimeSpan(entry1.Index),
                                Track2 = entry2.AudioTrack,
                                Track2Time = SubFingerprintIndexToTimeSpan(entry2.Index),
                                Source = matchSourceName
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

        private TimeSpan SubFingerprintIndexToTimeSpan(int index) {
            return new TimeSpan((long)Math.Round(index * profile.HashTimeScale * TimeUtil.SECS_TO_TICKS));
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
