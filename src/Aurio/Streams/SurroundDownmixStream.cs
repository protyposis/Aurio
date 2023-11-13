using Aurio.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Streams
{
    /// <summary>
    /// Downmixes surround (4.0, 5.1, 7.1) to stereo.
    /// </summary>
    public class SurroundDownmixStream : AbstractAudioStreamWrapper
    {
        private static readonly float LOWER_BY_3DB = (float)VolumeUtil.DecibelToLinear(-3);
        private static readonly float LOWER_BY_6DB = (float)VolumeUtil.DecibelToLinear(-6);

        private unsafe delegate void DownmixFunctionDelegate(
            float* sourceBuffer,
            float* targetBuffer,
            int samples
        );

        private AudioProperties properties;
        private ByteBuffer sourceBuffer;
        private DownmixFunctionDelegate DownmixFunction;

        /// <summary>
        /// Creates a SurroundDownmixStream that downmixes surround sound of the source stream and outputs a stereo stream.
        /// </summary>
        /// <param name="sourceStream">the stream to downmix to stereo</param>
        public SurroundDownmixStream(IAudioStream sourceStream)
            : base(sourceStream)
        {
            if (
                !(
                    sourceStream.Properties.Format == AudioFormat.IEEE
                    && sourceStream.Properties.BitDepth == 32
                )
            )
            {
                throw new ArgumentException(
                    "unsupported source format: " + sourceStream.Properties
                );
            }

            int sourceChannels = sourceStream.Properties.Channels;
            unsafe
            {
                if (sourceChannels == 4)
                {
                    // Assume 4.0 quad layout
                    DownmixFunction = DownmixQuad;
                }
                else if (sourceChannels == 6)
                {
                    // Assume 5.0/5.1 surround
                    DownmixFunction = Downmix51;
                }
                else if (sourceChannels == 8)
                {
                    // Assume 7.0/7.1
                    DownmixFunction = Downmix71;
                }
                else
                {
                    throw new Exception("Unsupported number of input channels: " + sourceChannels);
                }
            }

            properties = new AudioProperties(
                2,
                sourceStream.Properties.SampleRate,
                sourceStream.Properties.BitDepth,
                sourceStream.Properties.Format
            );
            sourceBuffer = new ByteBuffer();
        }

        public override AudioProperties Properties
        {
            get { return properties; }
        }

        public override long Length
        {
            get { return sourceStream.Length / sourceStream.SampleBlockSize * SampleBlockSize; }
        }

        public override long Position
        {
            get { return sourceStream.Position / sourceStream.SampleBlockSize * SampleBlockSize; }
            set { sourceStream.Position = value / SampleBlockSize * sourceStream.SampleBlockSize; }
        }

        public override int SampleBlockSize
        {
            get { return properties.SampleByteSize * properties.Channels; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int sourceChannels = sourceStream.Properties.Channels;
            int targetChannels = Properties.Channels;

            // TODO do this in consctructor, write different functions for downmixing and use a delegate to configure the function so we don't have to do this if/else at every call and know from the beginning if the layout is supported


            int sourceBufferSize = count / targetChannels * sourceChannels;
            int sourceBytesRead = sourceBuffer.FillIfEmpty(sourceStream, sourceBufferSize);
            // TODO downmix
            unsafe
            {
                fixed (
                    byte* sourceByteBuffer = &sourceBuffer.Data[0],
                        targetByteBuffer = &buffer[offset]
                )
                {
                    float* sourceFloatBuffer = (float*)sourceByteBuffer;
                    float* targetFloatBuffer = (float*)targetByteBuffer;
                    int samples = count / properties.SampleBlockByteSize;

                    DownmixFunction(sourceFloatBuffer, targetFloatBuffer, samples);
                }
            }

            sourceBuffer.Clear();

            return sourceBytesRead / sourceChannels * targetChannels;
        }

        // Downmixing internet resources
        // https://en.wikipedia.org/wiki/Surround_sound#Standard_speaker_channels
        // http://www.ac3filter.net/wiki/Speaker_layouts
        // http://www.tvtechnology.com/audio/0014/what-is-downmixing-part-1-stereo-loro/184912
        // https://www.cinemasound.com/3-issues-downmixing-5-1-stereo-adobe-audition/

        private static unsafe void DownmixQuad(
            float* sourceBuffer,
            float* targetBuffer,
            int samples
        )
        {
            for (int sampleNumber = 0; sampleNumber < samples; sampleNumber++)
            {
                int inputIndex = sampleNumber * 4;
                int outputIndex = sampleNumber * 2;

                float FL = sourceBuffer[inputIndex + 0];
                float FR = sourceBuffer[inputIndex + 1];
                float RL = sourceBuffer[inputIndex + 2];
                float RR = sourceBuffer[inputIndex + 3];

                float L = FL + RL * LOWER_BY_6DB;
                float R = FR + RR * LOWER_BY_6DB;

                targetBuffer[outputIndex + 0] = L;
                targetBuffer[outputIndex + 1] = R;
            }
        }

        private static unsafe void Downmix51(float* sourceBuffer, float* targetBuffer, int samples)
        {
            for (int sampleNumber = 0; sampleNumber < samples; sampleNumber++)
            {
                int inputIndex = sampleNumber * 6;
                int outputIndex = sampleNumber * 2;

                float FL = sourceBuffer[inputIndex + 0];
                float FR = sourceBuffer[inputIndex + 1];
                float FC = sourceBuffer[inputIndex + 2];
                // We ignore the LFE channel
                float RL = sourceBuffer[inputIndex + 4];
                float RR = sourceBuffer[inputIndex + 5];

                float L = FL + FC * LOWER_BY_3DB + RL * LOWER_BY_6DB;
                float R = FR + FC * LOWER_BY_3DB + RR * LOWER_BY_6DB;

                targetBuffer[outputIndex + 0] = L;
                targetBuffer[outputIndex + 1] = R;
            }
        }

        private static unsafe void Downmix71(float* sourceBuffer, float* targetBuffer, int samples)
        {
            for (int sampleNumber = 0; sampleNumber < samples; sampleNumber++)
            {
                int inputIndex = sampleNumber * 8;
                int outputIndex = sampleNumber * 2;

                float FL = sourceBuffer[inputIndex + 0];
                float FR = sourceBuffer[inputIndex + 1];
                float FC = sourceBuffer[inputIndex + 2];
                // We ignore the LFE channel
                float RL = sourceBuffer[inputIndex + 4];
                float RR = sourceBuffer[inputIndex + 5];
                float SL = sourceBuffer[inputIndex + 6];
                float SR = sourceBuffer[inputIndex + 7];

                float L = FL + FC * LOWER_BY_3DB + RL * LOWER_BY_6DB + SL * LOWER_BY_6DB;
                float R = FR + FC * LOWER_BY_3DB + RR * LOWER_BY_6DB + SR * LOWER_BY_6DB;

                targetBuffer[outputIndex + 0] = L;
                targetBuffer[outputIndex + 1] = R;
            }
        }
    }
}
