using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class DebugStream : AbstractAudioStreamWrapper {

        private long totalBytesRead;
        private long calculatedLength;

        public DebugStream(IAudioStream sourceStream)
            : base(sourceStream) {
                totalBytesRead = 0;
                calculatedLength = 0;
        }

        public DebugStream(IAudioStream sourceStream, DebugStreamController debugController)
            : this(sourceStream) {
                debugController.Add(this);
        }

        public string Name {
            get { return "DS(" + sourceStream.GetType().Name + ")"; }
        }

        public override long Position {
            get {
                return base.Position;
            }
            set {
                calculatedLength += value - base.Position;
                base.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = base.Read(buffer, offset, count);
            totalBytesRead += bytesRead;
            calculatedLength += bytesRead;

            if (bytesRead == 0) {
                Debug.WriteLine("EOS {0,-40}: pos {1}, len {2}, clen {3}, tbr {4}", 
                    Name, Position, Length, calculatedLength, totalBytesRead);
            }

            return bytesRead;
        }
    }
}
