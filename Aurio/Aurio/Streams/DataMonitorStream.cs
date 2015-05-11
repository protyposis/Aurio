using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace Aurio.Streams {
    public class DataMonitorStream : AbstractAudioStreamWrapper {

        public event EventHandler<StreamDataMonitorEventArgs> DataRead;

        public DataMonitorStream(IAudioStream sourceStream) : base(sourceStream) {
            if (sourceStream.Properties.BitDepth != 32 && sourceStream.Properties.Format != AudioFormat.IEEE) {
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
            }
        }

        public bool Disabled {
            get;
            set;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (Disabled) {
                return sourceStream.Read(buffer, offset, count);
            }

            int bytesRead = sourceStream.Read(buffer, offset, count);
            if (DataRead != null && bytesRead > 0) {
                DataRead(this, new StreamDataMonitorEventArgs(Properties, buffer, offset, bytesRead));
            }
            return bytesRead;
        }
    }

    public class StreamDataMonitorEventArgs : EventArgs {

        public StreamDataMonitorEventArgs(AudioProperties properties, byte[] buffer, int offset, int length) {
            Properties = properties;
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

        public AudioProperties Properties {
            get;
            private set;
        }
    }
}
