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
    /// A generic ring buffer with a fixed size.
    /// </summary>
    class RingBuffer<T>
    {
        private T[] buffer;
        private int bufferSize;
        private int bufferStart;
        private int bufferFillLevel;

        /// <summary>
        /// Instantiates a new ring buffer with the given size.
        /// </summary>
        public RingBuffer(int size)
        {
            this.buffer = new T[size];
            this.bufferSize = size;
            Clear();
        }

        /// <summary>
        /// Returns the capacity of the ring buffer.
        /// </summary>
        public int Length
        {
            get { return bufferSize; }
        }

        /// <summary>
        /// Returns the fill level of the ring buffer.
        /// </summary>
        public int Count
        {
            get { return bufferFillLevel; }
        }

        /// <summary>
        /// Adds a new element ot the ring buffer. When the buffer is already
        /// filled up to its capacity, the oldest element gets thrown away (FIFO style).
        /// </summary>
        public void Add(T data)
        {
            buffer[bufferStart] = data;
            bufferStart = (bufferStart + 1) % bufferSize;
            if (bufferFillLevel < bufferSize)
            {
                bufferFillLevel++;
            }
        }

        /// <summary>
        /// Removes the most recent addition from the ring buffer.
        /// </summary>
        public void RemoveHead()
        {
            bufferStart = Mod(bufferStart - 1, bufferSize);
            buffer[bufferStart] = default(T);
            if (bufferFillLevel > 0)
            {
                bufferFillLevel--;
            }
        }

        /// <summary>
        /// Removes the oldest addition from the ring buffer.
        /// </summary>
        public void RemoveTail()
        {
            if (bufferFillLevel > 0)
            {
                bufferFillLevel--;
            }
        }

        /// <summary>
        /// Gets an element from the ring buffer at the given index. The oldest element
        /// is always at index 0, the newest at Count-1.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">when the given size exceeds the fill level or the capacity</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= bufferFillLevel)
                {
                    throw new IndexOutOfRangeException();
                }
                int realIndex = (index + bufferStart + (bufferSize - bufferFillLevel)) % bufferSize;
                return buffer[realIndex];
            }
        }

        /// <summary>
        /// Clears the contents of the ring buffer.
        /// </summary>
        public void Clear()
        {
            bufferStart = 0;
            bufferFillLevel = 0;
        }

        private static int Mod(int i, int n)
        {
            return ((i %= n) < 0) ? i + n : i;
        }
    }
}
