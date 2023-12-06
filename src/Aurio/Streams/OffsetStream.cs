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

namespace Aurio.Streams
{
    public class OffsetStream : AbstractAudioStreamWrapper
    {
        private long position;
        private long offset;
        private bool positionOrOffsetChanged;

        public OffsetStream(IAudioStream sourceStream)
            : base(sourceStream)
        {
            position = 0;
            offset = 0;
        }

        public OffsetStream(IAudioStream sourceStream, long offset)
            : this(sourceStream)
        {
            Offset = offset;
        }

        public override long Length
        {
            get { return base.Length + Offset; }
        }

        public override long Position
        {
            get { return position; }
            set
            {
                position = value;
                positionOrOffsetChanged = true;
            }
        }

        public long Offset
        {
            get { return offset; }
            set
            {
                offset = value;
                positionOrOffsetChanged = true;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position >= Length)
            {
                return 0;
            }

            if (positionOrOffsetChanged)
            {
                sourceStream.Position = Position < Offset ? 0 : Position - Offset;
                positionOrOffsetChanged = false;
            }

            long byteOffset = Offset; // local value copy to avoid locking of the whole function
            int bytesRead = 0;

            if (position + count <= byteOffset)
            {
                // all requested data located in the offset interval -> return zeroed samples
                Array.Clear(buffer, offset, count);
                bytesRead = count;
            }
            else if (position < byteOffset)
            {
                // some requested data is located in the offset interval, and some in the source stream
                int offsetData = (int)(byteOffset - position);
                Array.Clear(buffer, offset, offsetData);
                bytesRead =
                    sourceStream.Read(buffer, offset + offsetData, count - offsetData) + offsetData;
            }
            else
            {
                // all requested data is located after the offset interval
                bytesRead = sourceStream.Read(buffer, offset, count);
            }

            position += bytesRead;
            return bytesRead;
        }
    }
}
