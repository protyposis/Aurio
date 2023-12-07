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
using System.Linq;

namespace Aurio.Streams
{
    /// <summary>
    /// A stream to concatenate multiple source streams sequentially into a single longer stream.
    /// </summary>
    public class ConcatenationStream : IAudioStream
    {
        private IAudioStream[] sourceStreams;

        private long length;
        private long positionOffset;
        private int currentStreamIndex;
        private IAudioStream currentStream;

        /// <summary>
        /// Creates a concatenated stream from the supplied source streams in the order of specification.
        /// </summary>
        /// <param name="sourceStreams">the source streams to concatenate in their given order</param>
        public ConcatenationStream(params IAudioStream[] sourceStreams)
        {
            this.sourceStreams = sourceStreams;

            currentStreamIndex = 0;
            currentStream = sourceStreams[currentStreamIndex];

            // Check for same format
            foreach (var stream in sourceStreams)
            {
                if (
                    stream.Properties.SampleRate != currentStream.Properties.SampleRate
                    || stream.Properties.BitDepth != currentStream.Properties.BitDepth
                    || stream.Properties.Channels != currentStream.Properties.Channels
                    || stream.Properties.Format != currentStream.Properties.Format
                )
                {
                    throw new ArgumentException("the formats of the supplied streams do not match");
                }
            }

            length = sourceStreams.Sum(s => s.Length);
            positionOffset = 0;
        }

        public AudioProperties Properties
        {
            get { return currentStream.Properties; }
        }

        public long Length
        {
            get { return length; }
        }

        public long Position
        {
            get { return positionOffset + currentStream.Position; }
            set
            {
                if (value < positionOffset || value >= positionOffset + currentStream.Length)
                {
                    // We need to switch to a different source stream
                    positionOffset = 0;
                    for (int i = 0; i < sourceStreams.Length; i++)
                    {
                        if (value < positionOffset + sourceStreams[i].Length)
                        {
                            currentStreamIndex = i;
                            currentStream = sourceStreams[i];
                            break;
                        }
                        positionOffset += sourceStreams[i].Length;
                    }
                    if (positionOffset == length)
                    {
                        // A seek to the end of the stream or beyond...
                        // ...we set the position to the end of the last stream instead of after the last stream
                        currentStreamIndex = sourceStreams.Length - 1;
                        currentStream = sourceStreams[currentStreamIndex];
                        positionOffset -= currentStream.Length;
                    }
                }

                // Seeking the current source stream
                currentStream.Position = value - positionOffset;
            }
        }

        public int SampleBlockSize
        {
            get { return currentStream.SampleBlockSize; }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (currentStream.Position == currentStream.Length)
            {
                if (currentStreamIndex == sourceStreams.Length - 1)
                {
                    // End of last stream
                    return 0;
                }

                // Switch to next source stream
                positionOffset += currentStream.Length;
                currentStream = sourceStreams[++currentStreamIndex];
                currentStream.Position = 0;
            }

            return currentStream.Read(buffer, offset, count);
        }

        public void Close()
        {
            foreach (IAudioStream sourceStream in sourceStreams)
            {
                sourceStream.Close();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
