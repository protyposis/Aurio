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

using Aurio.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Aurio.DataStructures
{
    static class ByteBufferExtensions
    {

        public static void ResizeIfTooSmall(this ByteBuffer buffer, int count)
        {
            if (buffer.Capacity < count)
            {
                int oldSize = buffer.Capacity;
                buffer.Resize(count);
                //Debug.WriteLine("buffer size increased: " + oldSize + " -> " + count);
            }
        }

        public static int Fill(this ByteBuffer buffer, IAudioStream sourceStream, int count)
        {
            int bytesRead = sourceStream.Read(buffer.Data, 0, count);
            buffer.Fill(bytesRead);
            return bytesRead;
        }

        public static int ForceFill(this ByteBuffer buffer, IAudioStream sourceStream, int count)
        {
            int bytesRead = StreamUtil.ForceRead(sourceStream, buffer.Data, 0, count);
            buffer.Fill(bytesRead);
            return bytesRead;
        }

        public static int ForceFill(this ByteBuffer buffer, IAudioStream sourceStream)
        {
            return ForceFill(buffer, sourceStream, buffer.Capacity);
        }

        public static int FillIfEmpty(this ByteBuffer buffer, IAudioStream sourceStream, int count)
        {
            if (buffer.Empty)
            {
                // dynamically increase buffer size
                // Here's a good place to resize the buffer because it is guaranteed to be empty / fully consumed
                ResizeIfTooSmall(buffer, count);

                // buffer is empty or all data has already been read -> refill
                return Fill(buffer, sourceStream, count);
            }

            return 0;
        }
    }
}
