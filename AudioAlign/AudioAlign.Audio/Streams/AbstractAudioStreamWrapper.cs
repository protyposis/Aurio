using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    public abstract class AbstractAudioStreamWrapper : IAudioStream {

        protected IAudioStream sourceStream;

        public AbstractAudioStreamWrapper(IAudioStream sourceStream) {
            this.sourceStream = sourceStream;
        }

        public virtual AudioProperties Properties {
            get { return sourceStream.Properties; }
        }

        public virtual long Length {
            get { return sourceStream.Length; }
        }

        public virtual long Position {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public virtual int SampleBlockSize {
            get { return sourceStream.SampleBlockSize; }
        }

        public virtual int Read(byte[] buffer, int offset, int count) {
            if (offset < 0 || offset >= buffer.Length || count < 0 || count > buffer.Length) {
                throw new ArgumentException("invalid parameters");
            }
            return sourceStream.Read(buffer, offset, count);
        }

        protected void ValidateSampleBlockAlignment(long value) {
            if (value % SampleBlockSize != 0) {
                throw new Exception("misaligned stream position (not aligned to the sample block size");
            }
        }
    }
}
