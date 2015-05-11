using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Aurio.DataStructures;

namespace Aurio.Streams {
    public class MonoStream : AbstractAudioStreamWrapper {

        private AudioProperties properties;
        private ByteBuffer sourceBuffer;
        private bool downmix;

        /// <summary>
        /// Creates a MonoStream that downmixes all channels of the source stream into a single mono channel.
        /// </summary>
        /// <param name="sourceStream">the stream to downmix to mono</param>
        public MonoStream(IAudioStream sourceStream)
            : this(sourceStream, 1) {
        }

        /// <summary>
        /// Creates a MonoStream that downmixes all channels of the source stream into a single mono channel
        /// and outputs the mono mix to multiple output channels.
        /// </summary>
        /// <param name="sourceStream">the stream to downmix to mono</param>
        /// <param name="outputChannels">the number of channel into which the mono mix should be split</param>
        public MonoStream(IAudioStream sourceStream, int outputChannels) : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }

            properties = new AudioProperties(outputChannels, sourceStream.Properties.SampleRate, 
                sourceStream.Properties.BitDepth, sourceStream.Properties.Format);
            sourceBuffer = new ByteBuffer();
            downmix = true;
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
            get { return properties.SampleByteSize * properties.Channels; }
        }

        public bool Downmix {
            get { return downmix; }
            set {
                if (value == false && sourceStream.Properties.Channels != properties.Channels) {
                    throw new Exception("downmixing can only be disabled if the number of input channels equals the number of output channels");
                }
                downmix = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (!downmix) {
                return sourceStream.Read(buffer, offset, count);
            }

            int sourceChannels = sourceStream.Properties.Channels;
            int targetChannels = Properties.Channels;

            /* NOTE: The buffer size must be at least of a size that can hold as much data as is required
             *       to generate the number of requested bytes. For a stereo to mono conversion, twice the
             *       amount of bytes needs to be read in order to generate the requested number of bytes.
             *       The other way, returning half the amount of bytes requested, doesn't work in some border
             *       cases, e.g. when just one 32bit sample is requested, because a request for 4 bytes from
             *       a 32bit stereo stream will result in zero bytes returned and it also could not be converted
             *       from stereo to mono (since 1 single sample isn't "stereo"). */
            int bufferSize = count * sourceChannels / targetChannels;
            int sourceBytesToRead = bufferSize - bufferSize % sourceStream.SampleBlockSize;
            int sourceBytesRead = sourceBuffer.FillIfEmpty(sourceStream, sourceBytesToRead);

            int sourceFloats = sourceBytesRead / 4;
            int sourceIndex = 0;
            int targetIndex = 0;
            float targetSample;

            unsafe {
                fixed (byte* sourceByteBuffer = &sourceBuffer.Data[0], targetByteBuffer = &buffer[offset]) {
                    float* sourceFloatBuffer = (float*)sourceByteBuffer;
                    float* targetFloatBuffer = (float*)targetByteBuffer;

                    while (sourceIndex < sourceFloats) {
                        targetSample = 0;
                        for (int ch = 0; ch < sourceChannels; ch++) {
                            targetSample += sourceFloatBuffer[sourceIndex++] / sourceChannels;
                        }
                        targetFloatBuffer[targetIndex++] = targetSample;
                        if (targetChannels > 1) {
                            for (int ch = 1; ch < targetChannels; ch++) {
                                targetFloatBuffer[targetIndex++] = targetSample;
                            }
                        }
                    }
                }
            }

            sourceBuffer.Clear();

            return sourceBytesRead / sourceChannels * targetChannels;
        }
    }
}
