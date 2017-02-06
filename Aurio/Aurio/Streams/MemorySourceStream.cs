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

namespace Aurio.Streams {

    /// <summary>
    /// A stream sourced from a <see cref="System.IO.MemoryStream"/>, which can also wrap a raw byte buffer.
    /// </summary>
    public class MemorySourceStream : IAudioStream {

        protected MemoryStream source;
        protected AudioProperties properties;

        public MemorySourceStream(MemoryStream source, AudioProperties properties) {
            this.source = source;
            this.properties = properties;
        }

        public AudioProperties Properties {
            get { return properties; }
        }

        public long Length {
            get { return source.Length; }
        }

        public long Position {
            get { return source.Position; }
            set { source.Position = value; }
        }

        public int SampleBlockSize {
            get { return properties.SampleBlockByteSize; }
        }

        public int Read(byte[] buffer, int offset, int count) {
            return source.Read(buffer, offset, count);
        }

        public void Close() {
            source.Close();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
