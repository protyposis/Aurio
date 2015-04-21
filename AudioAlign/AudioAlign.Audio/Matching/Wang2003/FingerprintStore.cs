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

            //

            return matches;
        }

        public List<Match> FindAllMatches() {
            List<Match> matches = new List<Match>();

            var collidingKeys = collisionMap.GetCollidingKeys();
            Debug.WriteLine("{0} colliding keys", collidingKeys.Count);

            foreach (FingerprintHash hash in collidingKeys) {
                matches.AddRange(FindMatches(hash));
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

            public List<FingerprintHash> hashes;
            public Dictionary<int, IndexEntry> index;

            public TrackStore() {
                hashes = new List<FingerprintHash>();
                index = new Dictionary<int, IndexEntry>();
            }
        }
    }
}
