using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching {
    class DictionaryCollisionMap : IFingerprintCollisionMap {

        private Dictionary<SubFingerprintHash, List<SubFingerprintLookupEntry>> lookupTable;

        public DictionaryCollisionMap() {
            lookupTable = new Dictionary<SubFingerprintHash, List<SubFingerprintLookupEntry>>();
        }

        public void Add(SubFingerprintHash subFingerprint, SubFingerprintLookupEntry lookupEntry) {
            if (!lookupTable.ContainsKey(subFingerprint)) {
                lookupTable.Add(subFingerprint, new List<SubFingerprintLookupEntry>());
            }
            lookupTable[subFingerprint].Add(lookupEntry);
        }

        public List<SubFingerprintHash> GetCollidingKeys() {
            List<SubFingerprintHash> subFingerprints = new List<SubFingerprintHash>();
            foreach (SubFingerprintHash subFingerprint in lookupTable.Keys) {
                if (lookupTable[subFingerprint].Count > 1) {
                    subFingerprints.Add(subFingerprint);
                }
            }
            return subFingerprints;
        }

        public List<SubFingerprintLookupEntry> GetValues(SubFingerprintHash subFingerprint) {
            return lookupTable[subFingerprint];
        }
    }
}
