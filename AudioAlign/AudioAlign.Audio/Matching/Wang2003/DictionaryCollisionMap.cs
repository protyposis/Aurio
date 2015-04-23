using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    /// <summary>
    /// Copied from <see cref="AudioAlign.Audio.Matching.HaitsmaKalker2002.DictionaryCollisionMap"/>
    /// </summary>
    class DictionaryCollisionMap : IFingerprintCollisionMap {

        private Dictionary<uint, List<FingerprintHashLookupEntry>> lookupTable;

        public DictionaryCollisionMap() {
            lookupTable = new Dictionary<uint, List<FingerprintHashLookupEntry>>();
        }

        public void Add(uint hash, FingerprintHashLookupEntry lookupEntry) {
            if (!lookupTable.ContainsKey(hash)) {
                lookupTable.Add(hash, new List<FingerprintHashLookupEntry>());
            }
            lookupTable[hash].Add(lookupEntry);
        }

        public List<uint> GetCollidingKeys() {
            List<uint> hashes = new List<uint>();
            foreach (uint hash in lookupTable.Keys) {
                if (lookupTable[hash].Count > 1) {
                    hashes.Add(hash);
                }
            }
            return hashes;
        }

        public List<FingerprintHashLookupEntry> GetValues(uint subFingerprint) {
            return lookupTable[subFingerprint];
        }
    }
}
