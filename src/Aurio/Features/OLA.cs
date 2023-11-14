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

namespace Aurio.Features
{
    /// <summary>
    /// Overlap-add. Writes a sequence of frames with the specified window size
    /// to a target stream, overlapped by the specified hop size.
    /// </summary>
    public class OLA
    {
        private readonly IAudioWriterStream stream;
        private readonly int windowSize;
        private readonly int hopSize;
        private readonly int overlapSize;

        /// <summary>
        /// Initializes a new overlap-adder for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to write the overlapped audio data to</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        public OLA(IAudioWriterStream stream, int windowSize, int hopSize)
        {
            if (stream.Properties.Format != AudioFormat.IEEE)
            {
                throw new ArgumentException("invalid stream format, IEEE expected");
            }
            if (stream.Properties.Channels > 1)
            {
                throw new ArgumentException("no multichannel support");
            }

            if (hopSize > windowSize)
            {
                throw new ArgumentOutOfRangeException("overlap cannot be larger than 100%");
            }

            this.stream = stream;
            this.windowSize = windowSize;
            this.hopSize = hopSize;
            this.overlapSize = windowSize - hopSize;

            Initialize();
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

        private byte[] buffer; // transfer buffer for the non-overlapping part
        private byte[] overlapBuffer;
        private int hopInBytes;
        private int overlapInBytes;
        private int nonoverlapInBytes;
        private bool flushed;

        private void Initialize()
        {
            hopInBytes = hopSize * stream.SampleBlockSize;
            overlapInBytes = overlapSize * stream.SampleBlockSize;
            nonoverlapInBytes = Math.Max(
                0,
                (windowSize - 2 * overlapSize) * stream.SampleBlockSize
            );
            buffer = new byte[nonoverlapInBytes];
            overlapBuffer = new byte[overlapInBytes];
            flushed = false;
            Array.Clear(overlapBuffer, 0, overlapBuffer.Length);
        }

        /// <summary>
        /// Writes a frame to the stream.
        /// </summary>
        /// <param name="frame">the source array where the frame will be read from</param>
        public virtual void WriteFrame(float[] frame)
        {
            if (flushed)
            {
                // After writing the last buffer through Flush, we cannot write any more frames
                throw new InvalidOperationException("already flushed");
            }
            if (frame.Length < windowSize)
            {
                throw new ArgumentException("the provided frame array has an invalid size");
            }

            // Add up the first overlap part
            if (overlapSize > 0)
            {
                unsafe
                {
                    fixed (byte* byteBuffer = &overlapBuffer[0])
                    {
                        float* floatBuffer = (float*)byteBuffer;

                        for (int i = 0; i < overlapSize; i++)
                        {
                            floatBuffer[i] += frame[i];
                        }
                    }
                }
            }

            // Write overlapped part to target stream
            if (hopSize < overlapSize)
            {
                stream.Write(overlapBuffer, 0, hopInBytes);
            }
            else
            {
                stream.Write(overlapBuffer, 0, overlapInBytes);
            }

            // When hop is less than overlap, overlap buffer is added up multiple times, but must be removed of the previously written part
            if (hopSize < overlapSize)
            {
                // Shift remaining overlap to the beginning of the array (BlockCopy handles overlapping elements)
                // This is more expensive but much simpler than implementing a circle buffer
                Buffer.BlockCopy(
                    overlapBuffer,
                    hopInBytes,
                    overlapBuffer,
                    0,
                    overlapInBytes - hopInBytes
                );
            }
            // Write nonoverlapped middle section, if existing
            else if (nonoverlapInBytes > 0)
            {
                Buffer.BlockCopy(frame, overlapInBytes, buffer, 0, nonoverlapInBytes);
                stream.Write(buffer, 0, nonoverlapInBytes);
            }

            // Write the second overlap part to the buffer for addition with next frame
            if (hopSize < overlapSize)
            {
                Buffer.BlockCopy(
                    frame,
                    overlapInBytes,
                    overlapBuffer,
                    overlapInBytes - hopInBytes,
                    hopInBytes
                );
            }
            else
            {
                Buffer.BlockCopy(frame, hopInBytes, overlapBuffer, 0, overlapInBytes);
            }

            OnFrameWritten(frame);
        }

        /// <summary>
        /// Writes the remaining buffer content (the last overlap part) to the target.
        /// </summary>
        public virtual void Flush()
        {
            if (flushed)
            {
                throw new InvalidOperationException("already flushed");
            }

            // Write overlapped part to target stream
            stream.Write(overlapBuffer, 0, overlapInBytes);
        }

        protected virtual void OnFrameWritten(float[] frame)
        {
            // to be overridden
        }
    }
}
