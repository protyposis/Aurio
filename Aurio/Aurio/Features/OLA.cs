using Aurio.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Features {
    /// <summary>
    /// Overlap-add. Writes a sequence of frames with the specified window size 
    /// to a target stream, overlapped by the specified hop size.
    /// </summary>
    public class OLA {

        private readonly IAudioWriterStream stream;
        private readonly int windowSize;
        private readonly int hopSize;

        /// <summary>
        /// Initializes a new overlap-adder for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to write the overlapped audio data to</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        public OLA(IAudioWriterStream stream, int windowSize, int hopSize) {
            if(stream.Properties.Format != AudioFormat.IEEE) {
                throw new ArgumentException("invalid stream format, IEEE expected");
            }
            if(stream.Properties.Channels > 1) {
                throw new ArgumentException("no multichannel support");
            }

            if(hopSize * 2 > windowSize) {
                throw new ArgumentOutOfRangeException("overlap cannot be larger than 50%");
            }

            this.stream = stream;
            this.windowSize = windowSize;
            this.hopSize = hopSize;

            Initialize();
        }

        /// <summary>
        /// Gets the audio properties of the stream.
        /// </summary>
        public AudioProperties StreamProperties {
            get { return stream.Properties; }
        }

        /// <summary>
        /// Gets the window size in samples.
        /// </summary>
        public int WindowSize {
            get { return windowSize; }
        }

        /// <summary>
        /// Gets the hop size in samples.
        /// </summary>
        public int HopSize {
            get { return hopSize; }
        }

        private byte[] buffer;
        private int overlapInBytes;
        private int nonoverlapInBytes;
        private bool flushed;

        private void Initialize() {
            overlapInBytes = hopSize * stream.SampleBlockSize;
            nonoverlapInBytes = (windowSize - 2 * hopSize) * stream.SampleBlockSize;
            buffer = new byte[overlapInBytes];
            flushed = false;
            Array.Clear(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a frame to the stream.
        /// </summary>
        /// <param name="frame">the source array where the frame will be read from</param>
        public virtual void WriteFrame(float[] frame) {
            if (flushed) {
                // After writing the last buffer through Flush, we cannot write any more frames
                throw new InvalidOperationException("already flushed");
            }
            if (frame.Length != windowSize) {
                throw new ArgumentException("the provided frame array has an invalid size");
            }

            // Add up the first overlap part
            unsafe
            {
                fixed (byte* byteBuffer = &buffer[0]) {
                    float* floatBuffer = (float*)byteBuffer;
                    
                    for (int i = 0; i < hopSize; i++) {
                        floatBuffer[i] += frame[i];
                    }
                }
            }

            // Write overlapped part to target stream
            stream.Write(buffer, 0, overlapInBytes);

            // Write nonoverlapped middle section, if existing
            if (nonoverlapInBytes > 0) {
                Buffer.BlockCopy(frame, overlapInBytes, buffer, 0, nonoverlapInBytes);
                stream.Write(buffer, 0, nonoverlapInBytes);
            }

            // Write the second overlap part to the buffer for addition with next frame
            Buffer.BlockCopy(frame, overlapInBytes + nonoverlapInBytes, buffer, 0, overlapInBytes);

            OnFrameWritten(frame);
        }

        /// <summary>
        /// Writes the remaining buffer content (the last overlap part) to the target.
        /// </summary>
        public virtual void Flush() {
            if(flushed) {
                throw new InvalidOperationException("already flushed");
            }

            // Write overlapped part to target stream
            stream.Write(buffer, 0, overlapInBytes);
        }

        protected virtual void OnFrameWritten(float[] frame) {
            // to be overridden
        }
    }
}
