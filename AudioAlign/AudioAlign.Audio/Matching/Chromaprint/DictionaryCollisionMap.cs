using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    class DictionaryCollisionMap : IFingerprintCollisionMap {

        private Dictionary<SubFingerprint, List<SubFingerprintLookupEntry>> lookupTable;

        public DictionaryCollisionMap() {
            lookupTable = new Dictionary<SubFingerprint, List<SubFingerprintLookupEntry>>();
        }

        public void Add(SubFingerprint subFingerprint, SubFingerprintLookupEntry lookupEntry) {
            if (!lookupTable.ContainsKey(subFingerprint)) {
                lookupTable.Add(subFingerprint, new List<SubFingerprintLookupEntry>());
            }
            lookupTable[subFingerprint].Add(lookupEntry);
        }

        public List<SubFingerprint> GetCollidingKeys() {
            List<SubFingerprint> subFingerprints = new List<SubFingerprint>();
            foreach (SubFingerprint subFingerprint in lookupTable.Keys) {
                if (lookupTable[subFingerprint].Count > 1) {
                    subFingerprints.Add(subFingerprint);
                }
            }
            return subFingerprints;
        }

        public List<SubFingerprintLookupEntry> GetValues(SubFingerprint subFingerprint) {
            return lookupTable[subFingerprint];
        }
    }
}
