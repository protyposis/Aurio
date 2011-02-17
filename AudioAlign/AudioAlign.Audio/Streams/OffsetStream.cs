using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    public class OffsetStream : AbstractAudioStreamWrapper {

        private long position;

        public OffsetStream(IAudioStream sourceStream)
            : base(sourceStream) {
            position = 0;
            Offset = 0;
        }

        public override long Length {
            get { return base.Length + Offset; }
        }

        public override long Position {
            get { return position; }
            set { position = value; }
        }

        public long Offset {
            get;
            set;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            long byteOffset = Offset; // local value copy to avoid locking of the whole function
            int bytesRead = 0;

            if (position + count < byteOffset) {
                // all requested data located in the offset interval
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
                sourceStream.Position = position - byteOffset;
                bytesRead = sourceStream.Read(buffer, offset, count);
            }

            position += bytesRead;
            return bytesRead;
        }
    }
}
