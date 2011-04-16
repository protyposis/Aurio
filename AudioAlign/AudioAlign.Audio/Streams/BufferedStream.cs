using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class BufferedStream : AbstractAudioStreamWrapper {

        private class Buffer {
            public byte[] data;
            public long streamPosition = 0;
            public long validDataLength = 0;
            public bool locked = false;

            public bool IsFilled {
                get { return validDataLength > 0; }
            }

            public void Clear() {
                streamPosition = 0;
                validDataLength = 0;
            }

            public bool Contains(long position) {
                return position >= streamPosition && position < streamPosition + validDataLength;
            }

            public bool Contains(long position, int count) {
                return position >= streamPosition && position + count <= streamPosition + validDataLength;
            }
        }

        private long position;
        private Buffer frontBuffer;
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
            frontBuffer = new Buffer() { data = new byte[bufferSize] };
            
            if (doubleBuffered) {
                backBuffer = new Buffer() { data = new byte[bufferSize] };
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
                long bufferCount = count;
                if (frontBuffer.validDataLength - bufferPosition < count) {
                    bufferCount = frontBuffer.validDataLength - bufferPosition;
                }
                Array.Copy(frontBuffer.data, bufferPosition, buffer, offset, bufferCount);
                position += bufferCount;
                return (int)bufferCount;
            }
            else {
                // fill buffer
                if (doubleBuffered && !backBuffer.locked && backBuffer.IsFilled) {
                    // switch buffers
                    CommonUtil.Swap<Buffer>(ref frontBuffer, ref backBuffer);
                    FillBufferAsync(backBuffer, frontBuffer.streamPosition + frontBuffer.validDataLength);
                }
                else {
                    FillBufferSync(frontBuffer, position);
                    if (doubleBuffered && !backBuffer.locked) {
                        FillBufferAsync(backBuffer, frontBuffer.streamPosition + frontBuffer.validDataLength);
                    }
                }

                // call the current function a second time... this time the buffer will contain the requested data
                return this.Read(buffer, offset, count);
            }
        }

        private void FillBufferSync(Buffer buffer, long position) {
            buffer.streamPosition = position;
            sourceStream.Position = position;
            buffer.validDataLength = StreamUtil.ForceRead(sourceStream, frontBuffer.data, 0, frontBuffer.data.Length);
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
