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
    /// <summary>
    /// An audio stream that returns no data. Useful for testing purposes.
    /// </summary>
    public class NullStream : IAudioStream
    {

        private AudioProperties audioProperties;
        private long length;
        private long position;

        public NullStream(AudioProperties audioProperties, long length)
        {
            this.audioProperties = audioProperties;
            this.length = length;
            this.position = 0;
        }

        #region IAudioStream Members

        public AudioProperties Properties
        {
            get { return audioProperties; }
        }

        public long Length
        {
            get { return length; }
        }

        public long Position
        {
            get { return position; }
            set { position = value; }
        }

        public int SampleBlockSize
        {
            get { return audioProperties.SampleBlockByteSize; }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead;

            if (position + count < length)
            {
                bytesRead = count;
            }
            else
            {
                bytesRead = (int)(length - position);
            }

            position += bytesRead;
            return bytesRead;
        }

        public void Close()
        {
            // nothing to release
        }

        #endregion

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
