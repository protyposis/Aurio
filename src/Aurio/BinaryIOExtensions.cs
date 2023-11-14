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
using System.IO;
using System.Linq;
using System.Text;

namespace Aurio
{
    public static class BinaryIOExtensions
    {
        public static void Write(this BinaryWriter bw, Peak peak)
        {
            bw.Write(peak.Min);
            bw.Write(peak.Max);
        }

        public static Peak ReadPeak(this BinaryReader br)
        {
            return new Peak(br.ReadSingle(), br.ReadSingle());
        }

        public static BinaryReader[] WrapWithBinaryReaders(this MemoryStream[] memoryStreams)
        {
            BinaryReader[] readers = new BinaryReader[memoryStreams.Length];
            for (int x = 0; x < memoryStreams.Length; x++)
            {
                readers[x] = new BinaryReader(memoryStreams[x]);
            }
            return readers;
        }

        public static BinaryWriter[] WrapWithBinaryWriters(this MemoryStream[] memoryStreams)
        {
            BinaryWriter[] writers = new BinaryWriter[memoryStreams.Length];
            for (int x = 0; x < memoryStreams.Length; x++)
            {
                writers[x] = new BinaryWriter(memoryStreams[x]);
            }
            return writers;
        }
    }
}
