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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Project;
using System.Diagnostics;
using Aurio.Streams;

namespace Aurio.Matching.HaitsmaKalker2002 {
    public class FingerprintStore {

        public const int DEFAULT_FINGERPRINT_SIZE = 256;
        public const float DEFAULT_THRESHOLD = 0.35f;

        private int fingerprintSize;
        private float threshold;
        private IProfile profile;
        private Dictionary<AudioTrack, List<SubFingerprintHash>> store;
        private IFingerprintCollisionMap collisionMap;
        private string matchSourceName;

        protected FingerprintStore(IProfile profile, IFingerprintCollisionMap collisionMap, string matchSourceName) {
            FingerprintSize = DEFAULT_FINGERPRINT_SIZE;
            Threshold = DEFAULT_THRESHOLD;
            this.profile = profile;
            store = new Dictionary<AudioTrack, List<SubFingerprintHash>>();

            this.collisionMap = collisionMap;

            /*
             * TODO to support processing of huge datasets (or machines with low memory),
             * the store could also be moved from Dictionary/Lists to SQLite, the database
             * written to disk (instead of in-memory like now) and the user given the 
             * choice between them (or automatically chosen depending on the amount of data).
             */

            this.matchSourceName = matchSourceName;
        }

        public FingerprintStore(IProfile profile, IFingerprintCollisionMap collisionMap) : this(profile, collisionMap, "FP-HK02") {
            //
        }

        public FingerprintStore(IProfile profile) : this(profile, new DictionaryCollisionMap(), "FP-HK02") {
            //
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

        public TimeSpan FingerprintDuration {
            get { return SubFingerprintIndexToTimeSpan(fingerprintSize); }
        }

        public IFingerprintCollisionMap CollisionMap {
            get { return collisionMap; }
        }

        public Dictionary<AudioTrack, List<SubFingerprintHash>> AudioTracks {
            get { return store; }
        }

        public void Add(AudioTrack audioTrack, List<SubFingerprint> subFingerprints, bool suppressSilentCollisions = false) {
            if (subFingerprints.Count == 0) {
                return;
            }

            lock (this) {
                if (!store.ContainsKey(audioTrack)) {
                    store.Add(audioTrack, new List<SubFingerprintHash>());
                }

                foreach (var sfp in subFingerprints) {
                    if (!sfp.IsVariation) {
                        // store the sub-fingerprint in the sequential list of the audio track
                        store[audioTrack].Add(sfp.Hash);
                    }

                    if (suppressSilentCollisions && sfp.Hash.Value == 0)
                    {
                        // Skip a silent hash, i.e. a hash without any changes, as it happens with silent signals as input
                        continue;
                    }

                    // insert a track/index lookup entry for the sub-fingerprint
                    collisionMap.Add(sfp.Hash, new SubFingerprintLookupEntry(audioTrack, sfp.Index));
                }
            }
        }

        public void Add(SubFingerprintsGeneratedEventArgs e, bool suppressSilentCollisions = false)
        {
            Add(e.AudioTrack, e.SubFingerprints, suppressSilentCollisions);
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
                                Source = matchSourceName
                            });
                        }
                    }
                }
                cycle++;
            }

            //Debug.WriteLine("finished");
            return matches;
        }

        public List<Match> FindAllMatches(Action<double> progressCallback) {
            List<Match> matches = new List<Match>();
            //collisionMap.CreateLookupIndex(); // TODO evaluate if this call speeds the process up
            //collisionMap.Cleanup();
            var collidingKeys = collisionMap.GetCollidingKeys();
            long total = collidingKeys.Count;
            long index = 0;
            foreach (SubFingerprintHash hash in collidingKeys) {
                // skip all hashes whose bits are all zero, since this is probably a position with silence
                if (hash.Value != 0) {
                    matches.AddRange(FindMatches(hash));
                }

                // Report progress
                if (++index % 1000 == 0 || index == total) {
                    progressCallback(100d / total * index);
                }
            }
            return matches;
        }

        public List<Match> FindAllMatches() {
            return FindAllMatches((progress) => { });
        }

        public Fingerprint GetFingerprint(SubFingerprintLookupEntry entry) {
            int indexOffset = 0;
            if (store[entry.AudioTrack].Count - entry.Index < fingerprintSize) {
                indexOffset = Math.Min(indexOffset, -fingerprintSize + store[entry.AudioTrack].Count - entry.Index);
            }
            return new Fingerprint(store[entry.AudioTrack], entry.Index + indexOffset, fingerprintSize);
        }

        public TimeSpan SubFingerprintIndexToTimeSpan(int index) {
            return new TimeSpan((long)Math.Round(index * profile.HashTimeScale * TimeUtil.SECS_TO_TICKS));
        }

        public List<Match> FindMatchesFromExternalSubFingerprints(AudioTrack audioTrack, List<SubFingerprintHash> hashes) {
            if (hashes.Count < this.fingerprintSize) {
                throw new ArgumentException(String.Format(
                    "Hash list is too short, cannot build fingerprints (given {0}, required at least {1})",
                    hashes.Count, this.fingerprintSize));
            }

            var matches = new List<Match>();
            int collisionCount = 0;

            for (int i = 0; i < hashes.Count; i++) {
                List<SubFingerprintLookupEntry> collisions = this.collisionMap.GetValues(hashes[i]);

                foreach (var collision in collisions) {
                    collisionCount++;

                    // The indices at which the fingerprints begin within the hash lists
                    int externalIndex = i;
                    int internalIndex = collision.Index;

                    // The hash lists
                    var externalHashes = hashes;
                    var internalHashes = store[collision.AudioTrack];

                    // The lengths of the hash lists (the number of subfingerprints/hashes)
                    int externalHashCount = externalHashes.Count;
                    int internalHashCount = internalHashes.Count;

                    // The overflow if directly taken at the indices
                    int externalOverflow = this.fingerprintSize - (externalHashCount - externalIndex);
                    int internalOverflow = this.fingerprintSize - (internalHashCount - internalIndex);

                    // The bigger of both overflows is the value by which we need to shift the sampling to the left
                    int leftShift = Math.Max(externalOverflow, internalOverflow);

                    // Check if we need to do a shift
                    // If the left shift is > 0, we need to shift the fingerprints to the left
                    // if the shift is <= 0, the fingerprints can be directly sampled from the hash lists 
                    // (the negative value is their distance to the right border)
                    if (leftShift > 0) {
                        // We need to shift the fingerprints to the left because one or both would otherwise overflow the right border
                        externalIndex -= leftShift;
                        internalIndex -= leftShift;

                        // Before we take the fingerprints, we need to check if that is even possible or if a fingerprint 
                        // would then overflow the left border
                        if (externalIndex < 0 || internalIndex < 0) {
                            // A fingerprint would now overflow the left border, so for this collision it is not possible
                            // to take fingerprints that we can compare
                            // Skip to the next collision
                            continue;
                        }
                    }

                    // TODO detect duplicate matching attempts and skip them to minimize matching cost
                    // A matching attempt can be described as the tuple (internalAudioTrack, externalIndex, internalIndex)

                    // Take the fingerprints that we want to compare
                    Fingerprint externalFingerprint = new Fingerprint(externalHashes, externalIndex, this.fingerprintSize);
                    // We don't use GetFingerprint here because that function shifts the fingerprint left for an unknown offset
                    // if taken at the right border
                    Fingerprint internalFingerprint = new Fingerprint(internalHashes, internalIndex, this.fingerprintSize);

                    // Calculate the bit error rate between both fingerprints
                    float bitErrorRate = Fingerprint.CalculateBER(externalFingerprint, internalFingerprint);

                    Console.WriteLine(String.Format("{0} <> {1} => {2}", externalIndex, internalIndex, bitErrorRate));

                    if (bitErrorRate < threshold) {
                        matches.Add(new Match {
                            Similarity = 1 - bitErrorRate,
                            Track1 = collision.AudioTrack,
                            Track1Time = SubFingerprintIndexToTimeSpan(internalIndex),
                            Track2 = audioTrack,
                            Track2Time = SubFingerprintIndexToTimeSpan(externalIndex),
                            Source = matchSourceName,
                        });
                    }
                }
            }

            Console.WriteLine(String.Format("{0} collisions, {1} matches", collisionCount, matches.Count));

            return matches;
        }
    }
}
