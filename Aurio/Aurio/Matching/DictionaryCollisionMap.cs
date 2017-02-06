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
