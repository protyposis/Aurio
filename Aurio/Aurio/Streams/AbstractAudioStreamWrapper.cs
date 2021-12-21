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

namespace Aurio.Streams
{
    public abstract class AbstractAudioStreamWrapper : IAudioStream
    {

        protected IAudioStream sourceStream;

        public AbstractAudioStreamWrapper(IAudioStream sourceStream)
        {
            this.sourceStream = sourceStream;
        }

        public virtual AudioProperties Properties
        {
            get { return sourceStream.Properties; }
        }

        public virtual long Length
        {
            get { return sourceStream.Length; }
        }

        public virtual long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public virtual int SampleBlockSize
        {
            get { return sourceStream.SampleBlockSize; }
        }

        public virtual int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || offset >= buffer.Length || count < 0 || count > buffer.Length)
            {
                throw new ArgumentException("invalid parameters");
            }
            return sourceStream.Read(buffer, offset, count);
        }

        public virtual void Close()
        {
            sourceStream.Close();
        }

        protected void ValidateSampleBlockAlignment(long value)
        {
            if (value % SampleBlockSize != 0)
            {
                throw new Exception("misaligned stream position (not aligned to the sample block size");
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
