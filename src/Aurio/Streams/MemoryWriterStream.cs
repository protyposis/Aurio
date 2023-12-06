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
using System.IO;

namespace Aurio.Streams
{
    public class MemoryWriterStream : MemorySourceStream, IAudioWriterStream
    {
        public MemoryWriterStream(MemoryStream target, AudioProperties properties)
            : base(target, properties) { }

        public MemoryWriterStream(AudioProperties properties)
            : base(new MemoryStream(), properties) { }

        public void Write(byte[] buffer, int offset, int count)
        {
            // Default stream checks according to MSDN
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer must not be null");
            }
            if (!source.CanWrite)
            {
                throw new NotSupportedException("target stream is not writable");
            }
            if (buffer.Length - offset < count)
            {
                throw new ArgumentException("not enough remaining bytes or count too large");
            }
            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException("offset and count must not be negative");
            }

            // Check block alignment
            if (count % SampleBlockSize != 0)
            {
                throw new ArgumentException("count must be a multiple of the sample block size");
            }

            source.Write(buffer, offset, count);
        }
    }
}
