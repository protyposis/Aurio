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
using SQLite;
using Aurio.Project;
using System.Diagnostics;

namespace Aurio.Matching {
    class SQLiteCollisionMap : IFingerprintCollisionMap {

        private class DTO {
            public UInt32 Hash { get; set; }
            public int TrackNumber { get; set; }
            public int TrackPositionIndex { get; set; }
        }

        private SQLiteConnection db;
        private Dictionary<AudioTrack, int> trackToNumber;
        private Dictionary<int, AudioTrack> numberToTrack;
        private List<DTO> insertBuffer;

        public SQLiteCollisionMap() {
            db = new SQLiteConnection(":memory:");
            db.CreateTable<DTO>();

            trackToNumber = new Dictionary<AudioTrack, int>();
            numberToTrack = new Dictionary<int, AudioTrack>();
            insertBuffer = new List<DTO>(1000);
        }

        public void Add(SubFingerprintHash hash, SubFingerprintLookupEntry lookupEntry) {
            int index = -1;

            if(!trackToNumber.TryGetValue(lookupEntry.AudioTrack, out index)) {
                index = trackToNumber.Count;
                trackToNumber.Add(lookupEntry.AudioTrack, index);
                numberToTrack.Add(index, lookupEntry.AudioTrack);
            }

            if(index == -1) {
                throw new Exception("something's wrong - this should not happen!!");
            }

            var dto = new DTO {
                Hash = hash.Value,
                TrackNumber = index,
                TrackPositionIndex = lookupEntry.Index
            };

            insertBuffer.Add(dto);

            if (insertBuffer.Count == 1000) {
                InsertBuffered();
            }

            //db.Insert();
        }

        private void InsertBuffered() {
            db.InsertAll(insertBuffer);
            insertBuffer.Clear();
        }

        public void CreateLookupIndex() {
            InsertBuffered();
            var start = DateTime.Now;
            db.Execute("create index if not exists DTO_Hash on DTO(Hash)");
            Debug.WriteLine("CreateLookupIndex duration: " + (DateTime.Now - start));
        }

        public void Cleanup() {
            InsertBuffered();

            int count = db.ExecuteScalar<int>("select count(*) from DTO");
            Debug.WriteLine("Cleanup count before: " + count);

            db.Execute("delete from DTO where Hash in (select Hash from (select Hash As Hash, COUNT(*) As Count from DTO group by Hash) where Count = 1)");

            count = db.ExecuteScalar<int>("select count(*) from DTO");
            Debug.WriteLine("Cleanup count after: " + count);
        }

        public List<SubFingerprintHash> GetCollidingKeys() {
            InsertBuffered();
            CreateLookupIndex();

            var start = DateTime.Now;
            IEnumerable<DTO> result = db.Query<DTO>("select * from (select Hash As Hash, COUNT(*) As Count from DTO group by Hash) where Count > 1");
            List<SubFingerprintHash> hashes = new List<SubFingerprintHash>();
            foreach (DTO dto in result) {
                hashes.Add(new SubFingerprintHash(dto.Hash));
            }
            Debug.WriteLine("GetCollidingHashes duration: " + (DateTime.Now - start));
            return hashes;
        }

        public List<SubFingerprintLookupEntry> GetValues(SubFingerprintHash hash) {
            if (insertBuffer.Count > 0) {
                InsertBuffered();
            }

            //var start = DateTime.Now;
            IEnumerable<DTO> result = db.Query<DTO>("select * from DTO where Hash = ?", hash.Value);
            List<SubFingerprintLookupEntry> lookupEntries = new List<SubFingerprintLookupEntry>();
            foreach (DTO dto in result) {
                lookupEntries.Add(new SubFingerprintLookupEntry(numberToTrack[dto.TrackNumber], dto.TrackPositionIndex));
            }
            //Debug.WriteLine("GetLookupEntries duration: " + (DateTime.Now - start));
            return lookupEntries;
        }
    }
}
