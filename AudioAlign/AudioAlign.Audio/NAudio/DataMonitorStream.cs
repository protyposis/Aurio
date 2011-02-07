using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.NAudio {
    public class DataMonitorStream : WaveStream {
        public WaveStream SourceStream { get; private set; }

        public event EventHandler<StreamDataMonitorEventArgs> DataRead;

        public DataMonitorStream(WaveStream sourceStream) {
            SourceStream = sourceStream;
            if (sourceStream.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
        }

        public override WaveFormat WaveFormat {
            get { return SourceStream.WaveFormat; }
        }

        public override long Length {
            get { return SourceStream.Length; }
        }

        public override long Position {
            get { return SourceStream.Position; }
            set { SourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = SourceStream.Read(buffer, offset, count);
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
