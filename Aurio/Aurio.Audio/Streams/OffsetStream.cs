using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Streams {
    public class OffsetStream : AbstractAudioStreamWrapper {

        private long position;
        private long offset;
        private bool positionOrOffsetChanged;

        public OffsetStream(IAudioStream sourceStream)
            : base(sourceStream) {
            position = 0;
            offset = 0;
        }

        public OffsetStream(IAudioStream sourceStream, long offset)
            : this(sourceStream) {
                Offset = offset;
        }

        public override long Length {
            get { return base.Length + Offset; }
        }

        public override long Position {
            get { return position; }
            set { 
                position = value;
                positionOrOffsetChanged = true;
            }
        }

        public long Offset {
            get { return offset; }
            set { 
                offset = value;
                positionOrOffsetChanged = true;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (Position >= Length) {
                return 0;
            }

            if (positionOrOffsetChanged) {
                sourceStream.Position = Position < Offset ? 0 : Position - Offset;
                positionOrOffsetChanged = false;
            }

            long byteOffset = Offset; // local value copy to avoid locking of the whole function
            int bytesRead = 0;

            if (position + count <= byteOffset) {
                // all requested data located in the offset interval -> return zeroed samples
                Array.Clear(buffer, offset, count);
                bytesRead = count;
            }
            else if (position < byteOffset) {
                // some requested data is located in the offset interval, and some in the source stream
                int offsetData = (int)(byteOffset - position);
                Array.Clear(buffer, offset, offsetData);
                bytesRead = sourceStream.Read(buffer, offset + offsetData, count - offsetData) + offsetData;
            }
            else {
                // all requested data is located after the offset interval
                bytesRead = sourceStream.Read(buffer, offset, count);
            }

            position += bytesRead;
            return bytesRead;
        }
    }
}
