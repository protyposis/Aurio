using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aurio.Streams {
    public class MemoryWriterStream : MemorySourceStream, IAudioWriterStream {
        public MemoryWriterStream(MemoryStream target, AudioProperties properties) :
            base(target, properties) {
        }

        public MemoryWriterStream(AudioProperties properties) :
            base(new MemoryStream(), properties) {
        }

        public void Write(byte[] buffer, int offset, int count) {
            // Default stream checks according to MSDN
            if(buffer == null) {
                throw new ArgumentNullException("buffer must not be null");
            }
            if(!source.CanWrite) {
                throw new NotSupportedException("target stream is not writable");
            }
            if(buffer.Length - offset < count) {
                throw new ArgumentException("not enough remaining bytes or count too large");
            }
            if(offset < 0 || count < 0) {
                throw new ArgumentOutOfRangeException("offset and count must not be negative");
            }

            // Check block alignment
            if(count % SampleBlockSize != 0) {
                throw new ArgumentException("count must be a multiple of the sample block size");
            }

            source.Write(buffer, offset, count);
        }
    }
}
