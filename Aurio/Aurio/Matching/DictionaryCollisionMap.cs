using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching {
    class DictionaryCollisionMap : IFingerprintCollisionMap {

        private Dictionary<SubFingerprintHash, List<SubFingerprintLookupEntry>> lookupTable;

        public DictionaryCollisionMap() {
            lookupTable = new Dictionary<SubFingerprintHash, List<SubFingerprintLookupEntry>>();
        }

        public void Add(SubFingerprintHash hash, SubFingerprintLookupEntry lookupEntry) {
            if (!lookupTable.ContainsKey(hash)) {
                lookupTable.Add(hash, new List<SubFingerprintLookupEntry>());
            }
            lookupTable[hash].Add(lookupEntry);
        }

        public List<SubFingerprintHash> GetCollidingKeys() {
            List<SubFingerprintHash> hashes = new List<SubFingerprintHash>();
            foreach (SubFingerprintHash hash in lookupTable.Keys) {
                if (lookupTable[hash].Count > 1) {
                    hashes.Add(hash);
                }
            }
            return hashes;
        }

        public List<SubFingerprintLookupEntry> GetValues(SubFingerprintHash hash) {
            return lookupTable[hash];
        }
    }
}
