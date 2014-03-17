using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class IeeeStream : AbstractAudioStreamWrapper {

        private AudioProperties properties;
        private byte[] sourceBuffer;
        private bool passthrough;

        public IeeeStream(IAudioStream sourceStream)
            : base(sourceStream) {
            if (sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32) {
                passthrough = true;
                properties = sourceStream.Properties;
            }
            else {
                if (!(sourceStream.Properties.Format == AudioFormat.LPCM && sourceStream.Properties.BitDepth == 16)) {
                    throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
                }

                properties = new AudioProperties(sourceStream.Properties.Channels, sourceStream.Properties.SampleRate,
                    32, AudioFormat.IEEE);

                sourceBuffer = new byte[0];
            }
        }

        public override AudioProperties Properties {
            get { return properties; }
        }

        public override long Length {
            get { return sourceStream.Length / sourceStream.SampleBlockSize * SampleBlockSize; }
        }

        public override long Position {
            get { return sourceStream.Position / sourceStream.SampleBlockSize * SampleBlockSize; }
            set { sourceStream.Position = value / SampleBlockSize * sourceStream.SampleBlockSize; }
        }

        public override int SampleBlockSize {
            get { return properties.SampleBlockByteSize; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (passthrough) {
                return sourceStream.Read(buffer, offset, count);
            }

            // dynamically increase buffer size
            if (sourceBuffer.Length < count) {
                int oldSize = sourceBuffer.Length;
                sourceBuffer = new byte[count];
                Debug.WriteLine("IeeeStream: buffer size increased: " + oldSize + " -> " + count);
            }

            int sourceBytesToRead = count / SampleBlockSize * sourceStream.SampleBlockSize;
            int sourceBytesRead = sourceStream.Read(sourceBuffer, 0, sourceBytesToRead);
            int samples = sourceBytesRead / 2; // #bytes / 2 = #shorts

            unsafe {
                fixed (byte* sourceByteBuffer = &sourceBuffer[0], targetByteBuffer = &buffer[offset]) {
                    short* sourceShortBuffer = (short*)sourceByteBuffer;
                    float* targetFloatBuffer = (float*)targetByteBuffer;

                    for (int x = 0; x < samples; x++) {
                        targetFloatBuffer[x] = (float)sourceShortBuffer[x] / short.MaxValue;
                    }
                }
            }

            return samples * 4;
        }
    }
}
