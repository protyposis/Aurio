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
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Aurio.DataStructures;

namespace Aurio.Streams {
    public class BufferedStream : AbstractAudioStreamWrapper {

        private class Buffer : ByteBuffer {

            /// <summary>
            /// The position in the source stream where the buffered data starts.
            /// </summary>
            public long streamPosition = 0;

            /// <summary>
            /// Tells if the buffer is being filled. If it is locked, data is being written to the buffer
            /// and no data should be read.
            /// </summary>
            public bool locked = false;

            public Buffer(int size)
                : base(size) {
                //
            }

            /// <summary>
            /// Clears the buffer and makes it empty.
            /// </summary>
            public new void Clear() {
                base.Clear();
                streamPosition = 0;
            }

            /// <summary>
            /// Returns true if a given position in the source stream is contained in the buffered data.
            /// </summary>
            /// <param name="position">the position in the source stream that should be checked for being buffered</param>
            /// <returns>true if the position is contained in the buffer, else false</returns>
            public bool Contains(long position) {
                return position >= streamPosition && position < streamPosition + Offset + Count;
            }

            /// <summary>
            /// Returns true if a given data interval of the source stream is contained in the buffered data.
            /// </summary>
            /// <param name="position">the start of the interval in the source stream that should be checked for being buffered</param>
            /// <param name="count">the length of the interval in the source stream that should be checked for being buffered</param>
            /// <returns>true if the interval is contained in the buffer, else false</returns>
            public bool Contains(long position, int count) {
                return position >= streamPosition && position + count <= streamPosition + Offset + Count;
            }
        }

        private long position;
        private Buffer frontBuffer; // the front buffer can always be read from, it never gets asynchronically accessed
        private Buffer backBuffer;
        private bool doubleBuffered;

        /// <summary>
        /// Creates a new buffered stream.
        /// </summary>
        /// <param name="sourceStream">the stream that should be buffered</param>
        /// <param name="bufferSize">the buffer size in bytes</param>
        /// <param name="doubleBuffered">true if the stream should be buffered by two buffers, 
        /// of which one gets asynchronously refilled while the other is being read from 
        /// (note: required memory will be bufferSize * 2)</param>
        public BufferedStream(IAudioStream sourceStream, int bufferSize, bool doubleBuffered)
            : base(sourceStream) {
            frontBuffer = new Buffer(bufferSize);
            
            if (doubleBuffered) {
                backBuffer = new Buffer(bufferSize);
            }

            position = sourceStream.Position;
            this.doubleBuffered = doubleBuffered;
        }

        public override long Position {
            get { return position; }
            set { position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (position >= sourceStream.Length) {
                return 0;
            }

            if (frontBuffer.Contains(position)) {
                // read data from buffer
                long bufferPosition = position - frontBuffer.streamPosition;
                if (frontBuffer.Length - bufferPosition < count) {
                    count = (int)(frontBuffer.Length - bufferPosition);
                }
                Array.Copy(frontBuffer.Data, bufferPosition, buffer, offset, count);
                position += count;
                return (int)count;
            }
            else if (doubleBuffered && !backBuffer.locked && !backBuffer.Empty && backBuffer.Contains(position)) {
                // swap buffers (requested data is contained in the back buffer so turn it to the front buffer)
                CommonUtil.Swap<Buffer>(ref frontBuffer, ref backBuffer);
                FillBufferAsync(backBuffer, frontBuffer.streamPosition + frontBuffer.Length);
            } else {
                // both buffers empty (happens at start or a position change beyond the buffered area, or at the end of the stream)
                FillBufferSync(frontBuffer, position);

                // If the buffer couldn't be filled with any data, the end of the stream is reached
                if (frontBuffer.Empty) {
                    return 0;
                }

                if (doubleBuffered && !backBuffer.locked) {
                    FillBufferAsync(backBuffer, frontBuffer.streamPosition + frontBuffer.Length);
                }
            }

            // call the current function a second time... this time the front buffer will contain the requested data
            return this.Read(buffer, offset, count);
        }

        [MethodImpl(MethodImplOptions.Synchronized)] // synchronized because we can only read once at a time from the same source stream
        private void FillBufferSync(Buffer buffer, long position) {
            buffer.Clear();
            buffer.streamPosition = position;
            sourceStream.Position = position;
            buffer.ForceFill(sourceStream, buffer.Capacity);
        }

        private void FillBufferAsync(Buffer buffer, long position) {
            if (buffer.locked) {
                throw new Exception("buffer is already locked!!");
            }
            buffer.locked = true;
            Task.Factory.StartNew(() => {
                FillBufferSync(buffer, position);
                buffer.locked = false;
            });
        }
    }
}
