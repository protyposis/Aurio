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

namespace Aurio.DataStructures
{
    /// <summary>
    /// A byte buffer to manage internal buffering in streams. This buffer
    /// requires to be filled when empty, and then be read sequentially until all
    /// filled data has been read. Fills are always "total", reads are "partial" until
    /// all data has been read.
    /// </summary>
    class ByteBuffer
    {

        private byte[] data;
        private int offset;
        private int count;

        /// <summary>
        /// Creates a new byte buffer with the supplied capacity.
        /// </summary>
        public ByteBuffer(int size)
        {
            data = new byte[size];
            Clear();
        }

        /// <summary>
        ///  Creates a new byte buffer with zero capacity.
        /// </summary>
        public ByteBuffer()
            : this(0)
        {
            //
        }

        /// <summary>
        /// Gets the number of bytes the buffer can hold.
        /// </summary>
        public int Capacity
        {
            get { return data.Length; }
        }

        /// <summary>
        /// Gets the byte array that holds the data managed by this class.
        /// </summary>
        public byte[] Data
        {
            get { return data; }
        }

        /// <summary>
        /// Gets the offset into the current data, which is the
        /// array index at which the next byte is going to be read.
        /// </summary>
        /// <example>
        /// Initially, the offset is always zero. When reading 10 bytes
        /// from a buffer with size >= 10, the offset moves to 10.
        /// </example>
        public int Offset
        {
            get { return offset; }
        }

        /// <summary>
        /// Gets the number of bytes left in the buffer to be read.
        /// </summary>
        public int Count
        {
            get { return count; }
        }

        /// <summary>
        /// Gets the total length of data in the buffer, indenpendent of the read offset.
        /// </summary>
        public int Length
        {
            get { return offset + count; }
        }

        /// <summary>
        /// Tells if there's unread data in the buffer.
        /// </summary>
        public bool Empty
        {
            get { return count == 0; }
        }

        /// <summary>
        /// Fills an empty buffer with the specified amount of bytes. 
        /// This only changes the internal buffer management state, actual writing
        /// has to be separately done on the <see cref="#Data"/> property.
        /// </summary>
        /// <param name="length">the number of bytes put inton the buffer</param>
        public void Fill(int length)
        {
            if (!Empty)
            {
                throw new InvalidOperationException("cannot fill a nonempty buffer");
            }

            offset = 0;
            count = length;
        }

        /// <summary>
        /// Reads the specified amount of bytes from the buffer.
        /// This only changes the internal buffer management state, actual reading
        /// has to be separately done on the <see cref="#Data"/> property.
        /// </summary>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>
        /// The number of bytes read; can be smaller than the supplied length 
        /// if the buffer contains less than the requested amount of data.
        /// </returns>
        public int Read(int length)
        {
            int lengthRead = Math.Min(length, count);

            offset += lengthRead;
            count -= lengthRead;

            return lengthRead;
        }

        /// <summary>
        /// Resizes the buffer to the supplied size, and optionally retains the remaining data.
        /// </summary>
        public void Resize(int newSize, bool retainData)
        {
            byte[] newData = new byte[newSize];

            if (retainData && !Empty)
            {
                if (count > newSize)
                {
                    // When there's more data remaining in the buffer than the new size can hold, retaining is not possible
                    throw new InvalidOperationException("retain data failed - the new buffer size is too small");
                }

                // Move the remaining data to offset 0 of the new buffer data (count stays the same)
                Array.Copy(data, offset, newData, 0, count);
                offset = 0;
            }
            else
            {
                Clear();
            }

            data = newData;
        }

        /// <summary>
        /// Resizes the buffer to the supplied size, clearing all data.
        /// </summary>
        public void Resize(int newSize)
        {
            Resize(newSize, false);
        }

        /// <summary>
        /// Clears the data from the buffer. This only changes the internal buffer
        /// management state, it doesn't clear the actual data array itself.
        /// </summary>
        public void Clear()
        {
            offset = 0;
            count = 0;
        }
    }
}
