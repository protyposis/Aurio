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
using System.IO;
using System.Linq;
using System.Text;

namespace Aurio.Streams
{
    /// <summary>
    /// A stream sourced from a <see cref="System.IO.MemoryStream"/> (which can wrap a raw byte buffer), or a float array.
    /// </summary>
    public class MemorySourceStream : IAudioStream
    {
        protected MemoryStream source;
        protected AudioProperties properties;

        public MemorySourceStream(MemoryStream source, AudioProperties properties)
        {
            this.source = source;
            this.properties = properties;
        }

        /// <summary>
        /// Create a stream from an array of float samples in IEEE format.
        /// Creates a copy of the given sample array.
        /// </summary>
        /// <param name="samples">the array containing the samples</param>
        /// <param name="sampleRate">the sample rate of the samples</param>
        /// <param name="channels">the number of channels of interleaved samples</param>
        public MemorySourceStream(float[] samples, int sampleRate, int channels)
        {
            var buffer = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);
            var ms = new MemoryStream(buffer);

            this.source = ms;
            this.properties = new AudioProperties(channels, sampleRate, 32, AudioFormat.IEEE);
        }

        public AudioProperties Properties
        {
            get { return properties; }
        }

        public virtual long Length
        {
            get { return source.Length; }
        }

        public virtual long Position
        {
            get { return source.Position; }
            set { source.Position = value; }
        }

        public int SampleBlockSize
        {
            get { return properties.SampleBlockByteSize; }
        }

        public virtual int Read(byte[] buffer, int offset, int count)
        {
            return source.Read(buffer, offset, count);
        }

        public void Close()
        {
            source.Close();
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
