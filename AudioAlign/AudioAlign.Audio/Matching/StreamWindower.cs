using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AudioAlign.Audio.Matching {
    /// <summary>
    /// Reads consecutive windows from a stream, specified by the window size and hop size.
    /// </summary>
    class StreamWindower {

        private readonly IAudioStream stream;
        private readonly int windowSize;
        private readonly int hopSize;

        /// <summary>
        /// Initializes a new windower for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        public StreamWindower(IAudioStream stream, int windowSize, int hopSize) {
            this.stream = stream;
            this.windowSize = windowSize;
            this.hopSize = hopSize;

            Initialize();
        }

        /// <summary>
        /// Initializes a new windower for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of time</param>
        /// <param name="hopSize">the hop size in the dimension of time</param>
        public StreamWindower(IAudioStream stream, TimeSpan windowSize, TimeSpan hopSize) {
            this.stream = stream;
            this.windowSize = (int)TimeUtil.TimeSpanToBytes(windowSize, stream.Properties) / stream.Properties.SampleByteSize;
            this.hopSize = (int)TimeUtil.TimeSpanToBytes(hopSize, stream.Properties) / stream.Properties.SampleByteSize;

            // calculate next power of 2 for the window size as required by the FFT
            // http://stackoverflow.com/questions/264080/how-do-i-calculate-the-closest-power-of-2-or-10-a-number-is
            // http://acius2.blogspot.com/2007/11/calculating-next-power-of-2.html
            int windowSizePower2 = this.windowSize;
            windowSizePower2--;
            windowSizePower2 = (windowSizePower2 >> 1) | windowSizePower2;
            windowSizePower2 = (windowSizePower2 >> 2) | windowSizePower2;
            windowSizePower2 = (windowSizePower2 >> 4) | windowSizePower2;
            windowSizePower2 = (windowSizePower2 >> 8) | windowSizePower2;
            windowSizePower2 = (windowSizePower2 >> 16) | windowSizePower2;
            windowSizePower2++;
            if (this.windowSize < windowSizePower2) {
                Debug.WriteLine("window size enlarged to the next power of 2 as required by FFT: {0} -> {1}",
                    this.windowSize, windowSizePower2);
                this.windowSize = windowSizePower2;
            }

            Initialize();
        }

        /// <summary>
        /// Gets the audio properties of the stream.
        /// </summary>
        public AudioProperties StreamProperties {
            get { return stream.Properties; }
        }

        /// <summary>
        /// Gets the window size in samples;
        /// </summary>
        public int WindowSize {
            get { return windowSize; }
        }

        /// <summary>
        /// Gets the hop size in samples;
        /// </summary>
        public int HopSize {
            get { return hopSize; }
        }

        private const int STREAM_INPUT_BUFFER_SIZE = 32768;

        private byte[] streamBuffer;
        private int streamBufferOffset;
        private int streamBufferLevel;
        private int frameSize;
        private int frameOffset;
        private int hopSizeB;

        private void Initialize() {
            if (windowSize > STREAM_INPUT_BUFFER_SIZE) {
                throw new ArgumentException("window size is too large - doesn't fit into the internal buffer");
            }

            int sampleBytes = stream.Properties.SampleByteSize;
            streamBuffer = new byte[STREAM_INPUT_BUFFER_SIZE * sampleBytes];
            streamBufferOffset = 0;
            streamBufferLevel = 0;
            frameSize = windowSize * sampleBytes;
            frameOffset = 0;
            hopSizeB = hopSize * sampleBytes;
        }

        //private TimeSpan timestamp = TimeSpan.Zero;

        /// <summary>
        /// Checks if there's another frame to read.
        /// </summary>
        /// <returns>true if there's a frame to read, false if the end of the stream has been reached</returns>
        public bool HasNext() {
            return (frameOffset + frameSize <= streamBufferLevel) || // either there's another frame in the buffer
                frameSize <= ((streamBufferLevel - frameOffset) + // or the remaining buffer data plus the remaining stream data hold another frame
                    (stream.Length - stream.Position));
        }

        /// <summary>
        /// Fills the stream buffer.
        /// </summary>
        /// <returns>true if the buffer has been filled, false if the end of the stream has been reached</returns>
        private bool FillBuffer() {
            // first, carry over unprocessed samples from the end of the stream buffer to its beginning
            streamBufferOffset = streamBufferLevel - frameOffset;
            if (streamBufferOffset > 0) {
                Buffer.BlockCopy(streamBuffer, frameOffset, streamBuffer, 0, streamBufferOffset);
            }
            frameOffset = 0;

            // second, fill the stream input buffer - if no bytes returned we have reached the end of the stream
            streamBufferLevel = StreamUtil.ForceRead(stream, streamBuffer,
                streamBufferOffset, streamBuffer.Length - streamBufferOffset);
            if (streamBufferLevel == 0) {
                Debug.WriteLine("stream windowing finished - end position {0}/{1}", stream.Position, stream.Length);
                return false; // whole stream has been processed
            }
            streamBufferLevel += streamBufferOffset;
            streamBufferOffset = 0;

            return true; // stream buffer successfully filled
        }

        /// <summary>
        /// Reads a frame from the stream.
        /// </summary>
        /// <param name="frame">the target array where the frame will be copied to</param>
        public virtual void ReadFrame(float[] frame) {
            if (frame.Length != windowSize) {
                throw new ArgumentException("the provided frame array has an invalid size");
            }
            if (frameOffset + frameSize > streamBufferLevel) {
                // if there's no more frame in the stream buffer, refill it
                if (!FillBuffer()) {
                    throw new Exception("end of stream reached - no more frames to read");
                }
            }
            // copy window to frame buffer
            Buffer.BlockCopy(streamBuffer, frameOffset, frame, 0, frameSize);
            frameOffset += hopSizeB;
        }
    }
}
