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

namespace Aurio.Streams
{
    public interface IAudioWriterStream : IAudioStream
    {
        /// <summary>
        /// Writes audio byte data into the stream.
        /// The number of bytes to write should be a multiple of SampleBlockSize.
        /// </summary>
        /// <param name="buffer">the source buffer</param>
        /// <param name="offset">the offset in the source buffer</param>
        /// <param name="count">the number of bytes to write</param>
        void Write(byte[] buffer, int offset, int count);
    }
}
