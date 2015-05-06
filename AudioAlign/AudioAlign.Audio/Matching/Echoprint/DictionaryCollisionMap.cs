using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    /// <summary>
    /// Copied from <see cref="AudioAlign.Audio.Matching.HaitsmaKalker2002.DictionaryCollisionMap"/>
    /// </summary>
    class DictionaryCollisionMap : IFingerprintCollisionMap {

        private Dictionary<SubFingerprint, List<FingerprintHashLookupEntry>> lookupTable;

        public DictionaryCollisionMap() {
            lookupTable = new Dictionary<SubFingerprint, List<FingerprintHashLookupEntry>>();
        }

        public void Add(SubFingerprint hash, FingerprintHashLookupEntry lookupEntry) {
            if (!lookupTable.ContainsKey(hash)) {
                lookupTable.Add(hash, new List<FingerprintHashLookupEntry>());
            }
            lookupTable[hash].Add(lookupEntry);
        }

        public List<SubFingerprint> GetCollidingKeys() {
            List<SubFingerprint> hashes = new List<SubFingerprint>();
            foreach (SubFingerprint hash in lookupTable.Keys) {
                if (lookupTable[hash].Count > 1) {
                    hashes.Add(hash);
                }
            }
            return hashes;
        }

        public List<FingerprintHashLookupEntry> GetValues(SubFingerprint subFingerprint) {
            return lookupTable[subFingerprint];
        }
    }
}
