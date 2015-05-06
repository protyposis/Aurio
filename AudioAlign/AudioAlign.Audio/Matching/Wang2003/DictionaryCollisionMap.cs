using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    /// <summary>
    /// Copied from <see cref="AudioAlign.Audio.Matching.HaitsmaKalker2002.DictionaryCollisionMap"/>
    /// </summary>
    class DictionaryCollisionMap : IFingerprintCollisionMap {

        private Dictionary<SubFingerprint, List<SubFingerprintLookupEntry>> lookupTable;

        public DictionaryCollisionMap() {
            lookupTable = new Dictionary<SubFingerprint, List<SubFingerprintLookupEntry>>();
        }

        public void Add(SubFingerprint hash, SubFingerprintLookupEntry lookupEntry) {
            if (!lookupTable.ContainsKey(hash)) {
                lookupTable.Add(hash, new List<SubFingerprintLookupEntry>());
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

        public List<SubFingerprintLookupEntry> GetValues(SubFingerprint subFingerprint) {
            return lookupTable[subFingerprint];
        }
    }
}
