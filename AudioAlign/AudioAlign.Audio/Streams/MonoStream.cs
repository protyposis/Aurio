using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class MonoStream : AbstractAudioStreamWrapper {

        private AudioProperties properties;
        private byte[] sourceBuffer;

        public MonoStream(IAudioStream sourceStream) : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }

            properties = new AudioProperties(1, sourceStream.Properties.SampleRate, 
                sourceStream.Properties.BitDepth, sourceStream.Properties.Format);
            sourceBuffer = new byte[0];
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
            get { return properties.SampleByteSize; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            // dynamically increase buffer size
            if (sourceBuffer.Length < count) {
                int oldSize = sourceBuffer.Length;
                sourceBuffer = new byte[count];
                Debug.WriteLine("MonoStream: buffer size increased: " + oldSize + " -> " + count);
            }

            int sourceBytesRead = sourceStream.Read(sourceBuffer, 0, count);

            int sourceChannels = sourceStream.Properties.Channels;
            int sourceFloats = sourceBytesRead / 4;
            int sourceIndex = 0;
            int targetIndex = 0;
            float targetSample;

            unsafe {
                fixed (byte* sourceByteBuffer = &sourceBuffer[0], targetByteBuffer = &buffer[offset]) {
                    float* sourceFloatBuffer = (float*)sourceByteBuffer;
                    float* targetFloatBuffer = (float*)targetByteBuffer;

                    while (sourceIndex < sourceFloats) {
                        targetSample = 0;
                        for (int ch = 0; ch < sourceChannels; ch++) {
                            targetSample += sourceFloatBuffer[sourceIndex++] / sourceChannels;
                        }
                        targetFloatBuffer[targetIndex++] = targetSample;
                    }
                }
            }

            return sourceBytesRead / sourceChannels;
        }
    }
}
