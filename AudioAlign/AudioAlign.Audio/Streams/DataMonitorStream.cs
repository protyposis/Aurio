using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class DataMonitorStream : AbstractAudioStreamWrapper {

        public event EventHandler<StreamDataMonitorEventArgs> DataRead;

        public DataMonitorStream(IAudioStream sourceStream) : base(sourceStream) {
            if (sourceStream.Properties.BitDepth != 32 && sourceStream.Properties.Format != AudioFormat.IEEE) {
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = sourceStream.Read(buffer, offset, count);
            if (DataRead != null) {
                DataRead(this, new StreamDataMonitorEventArgs(buffer, offset, bytesRead));
            }
            return bytesRead;
        }
    }

    public class StreamDataMonitorEventArgs : EventArgs {

        public StreamDataMonitorEventArgs(byte[] buffer, int offset, int length) {
            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        public byte[] Buffer {
            get;
            private set;
        }

        public int Offset {
            get;
            private set;
        }

        public int Length {
            get;
            private set;
        }
    }
}
