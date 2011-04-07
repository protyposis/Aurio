using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    public class CropStream : AbstractAudioStreamWrapper {

        private long begin, end;

        public CropStream(IAudioStream sourceStream)
            : base(sourceStream) {
            this.begin = 0;
            this.end = sourceStream.Length;
        }

        public CropStream(IAudioStream sourceStream, long begin, long end)
            : this(sourceStream) {
            ValidateCropBounds(begin, end);
            this.begin = begin;
            this.end = end;
        }

        private void ValidateCropBounds(long begin, long end) {
            if (begin < 0 || begin > sourceStream.Length) {
                throw new ArgumentOutOfRangeException("begin");
            }
            if (end < 0 || end > sourceStream.Length) {
                throw new ArgumentOutOfRangeException("end");
            }
            if (begin > end) {
                throw new ArgumentOutOfRangeException("begin after end");
            }
        }

        public long Begin {
            get { return begin; }
            set { 
                ValidateCropBounds(value, end); 
                ValidateSampleBlockAlignment(value);
                begin = value;
            }
        }

        public long End {
            get { return end; }
            set { 
                ValidateCropBounds(begin, value); 
                ValidateSampleBlockAlignment(value); 
                end = value; 
            }
        }

        public override long Position {
            get { return base.Position - begin; }
            set { base.Position = value + begin; }
        }

        public override long Length {
            get { return end - begin; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return base.Read(buffer, offset, Length - Position < count ? (int)(Length - Position) : count);
        }
    }
}
