using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    /// <summary>
    /// Copied from <see cref="AudioAlign.Audio.Matching.HaitsmaKalker2002.DictionaryCollisionMap"/>
    /// </summary>
    class DictionaryCollisionMap : IFingerprintCollisionMap {

        private Dictionary<FingerprintHash, List<FingerprintHashLookupEntry>> lookupTable;

        public DictionaryCollisionMap() {
            lookupTable = new Dictionary<FingerprintHash, List<FingerprintHashLookupEntry>>();
        }

        public void Add(FingerprintHash hash, FingerprintHashLookupEntry lookupEntry) {
            if (!lookupTable.ContainsKey(hash)) {
                lookupTable.Add(hash, new List<FingerprintHashLookupEntry>());
            }
            lookupTable[hash].Add(lookupEntry);
        }

        public List<FingerprintHash> GetCollidingKeys() {
            List<FingerprintHash> hashes = new List<FingerprintHash>();
            foreach (FingerprintHash hash in lookupTable.Keys) {
                if (lookupTable[hash].Count > 1) {
                    hashes.Add(hash);
                }
            }
            return hashes;
        }

        public List<FingerprintHashLookupEntry> GetValues(FingerprintHash subFingerprint) {
            return lookupTable[subFingerprint];
        }
    }
}
