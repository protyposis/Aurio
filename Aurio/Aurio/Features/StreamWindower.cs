// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Streams;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Aurio.Features
{
    /// <summary>
    /// Reads consecutive windows from a stream, specified by the window size and hop size.
    /// </summary>
    public class StreamWindower
    {

        public const int DEFAULT_STREAM_INPUT_BUFFER_SIZE = 32768;

        private readonly IAudioStream stream;
        private readonly int windowSize;
        private readonly int hopSize;
        private readonly WindowFunction windowFunction;
        private readonly int bufferSize;

        /// <summary>
        /// Initializes a new windower for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        /// <param name="windowType">the type of the window function to apply</param>
        public StreamWindower(IAudioStream stream, int windowSize, int hopSize, WindowType windowType, int bufferSize = DEFAULT_STREAM_INPUT_BUFFER_SIZE)
        {
            this.stream = stream;
            this.windowSize = windowSize;
            this.hopSize = hopSize;
            this.windowFunction = WindowUtil.GetFunction(windowType, WindowSize);
            this.bufferSize = bufferSize;

            Initialize();
        }

        /// <summary>
        /// Initializes a new windower for the specified stream with the specified window and hop size and a rectangular window function.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        public StreamWindower(IAudioStream stream, int windowSize, int hopSize, int bufferSize = DEFAULT_STREAM_INPUT_BUFFER_SIZE)
            : this(stream, windowSize, hopSize, WindowType.Rectangle, bufferSize)
        {
        }

        /// <summary>
        /// Initializes a new windower for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of time</param>
        /// <param name="hopSize">the hop size in the dimension of time</param>
        /// <param name="windowType">the type of the window function to apply</param>
        public StreamWindower(IAudioStream stream, TimeSpan windowSize, TimeSpan hopSize, WindowType windowType, int bufferSize = DEFAULT_STREAM_INPUT_BUFFER_SIZE)
        {
            this.stream = stream;
            this.windowSize = (int)TimeUtil.TimeSpanToBytes(windowSize, stream.Properties) / stream.Properties.SampleByteSize;
            this.hopSize = (int)TimeUtil.TimeSpanToBytes(hopSize, stream.Properties) / stream.Properties.SampleByteSize;
            this.bufferSize = bufferSize;

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
            if (this.windowSize < windowSizePower2)
            {
                Debug.WriteLine("window size enlarged to the next power of 2 as required by FFT: {0} -> {1}",
                    this.windowSize, windowSizePower2);
                this.windowSize = windowSizePower2;
            }

            Initialize();
        }

        /// <summary>
        /// Initializes a new windower for the specified stream with the specified window and hop size and a rectangular window function.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of time</param>
        /// <param name="hopSize">the hop size in the dimension of time</param>
        public StreamWindower(IAudioStream stream, TimeSpan windowSize, TimeSpan hopSize, int bufferSize = DEFAULT_STREAM_INPUT_BUFFER_SIZE)
            : this(stream, windowSize, hopSize, WindowType.Rectangle, bufferSize)
        {
        }

        /// <summary>
        /// Gets the audio properties of the stream.
        /// </summary>
        public AudioProperties StreamProperties
        {
            get { return stream.Properties; }
        }

        /// <summary>
        /// Gets the window size in samples.
        /// </summary>
        public int WindowSize
        {
            get { return windowSize; }
        }

        /// <summary>
        /// Gets the hop size in samples.
        /// </summary>
        public int HopSize
        {
            get { return hopSize; }
        }

        /// <summary>
        /// Gets the total number of windows/frames that can be read.
        /// </summary>
        public virtual int WindowCount
        {
            get { return (int)(((stream.Length / stream.Properties.SampleBlockByteSize) - WindowSize) / HopSize) + 1; }
        }

        private byte[] streamBuffer;
        private int streamBufferOffset;
        private int streamBufferLevel;
        private int frameSize;
        private int frameOffset;
        private int hopSizeB;

        private void Initialize()
        {
            if (windowSize > this.bufferSize)
            {
                throw new ArgumentException("window size is too large - doesn't fit into the internal buffer");
            }

            int sampleBytes = stream.Properties.SampleByteSize;
            streamBuffer = new byte[this.bufferSize * sampleBytes];
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
        public bool HasNext()
        {
            return (frameOffset + frameSize <= streamBufferLevel) || // either there's another frame in the buffer
                FillBuffer(); // or we try to read the next frame and check if it succeeded
        }

        /// <summary>
        /// Fills the stream buffer.
        /// </summary>
        /// <returns>true if the buffer has been filled, false if the end of the stream has been reached</returns>
        private bool FillBuffer()
        {
            // first, carry over unprocessed samples from the end of the stream buffer to its beginning
            streamBufferOffset = streamBufferLevel - frameOffset;
            if (streamBufferOffset > 0)
            {
                Buffer.BlockCopy(streamBuffer, frameOffset, streamBuffer, 0, streamBufferOffset);
            }
            frameOffset = 0;

            // second, fill the stream input buffer - if no bytes returned we have reached the end of the stream
            streamBufferLevel = StreamUtil.ForceRead(stream, streamBuffer,
                streamBufferOffset, streamBuffer.Length - streamBufferOffset);
            if (streamBufferLevel == 0)
            {
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
        public virtual void ReadFrame(float[] frame)
        {
            if (frame.Length < windowSize)
            {
                throw new ArgumentException("the provided frame array has an invalid size");
            }
            if (frameOffset + frameSize > streamBufferLevel)
            {
                // if there's no more frame in the stream buffer, refill it
                if (!FillBuffer())
                {
                    // This case only happens if HasNext() is not used to check if there actually is another frame
                    throw new Exception("end of stream reached - no more frames to read");
                }
            }
            // copy window to frame buffer
            Buffer.BlockCopy(streamBuffer, frameOffset, frame, 0, frameSize);
            frameOffset += hopSizeB;

            // apply window function
            windowFunction.Apply(frame);

            OnFrameRead(frame);
        }

        protected virtual void OnFrameRead(float[] frame)
        {
            // to be overridden
        }
    }
}
