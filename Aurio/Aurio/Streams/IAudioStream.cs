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
    /// An audio stream.
    /// </summary>
    public interface IAudioStream : IDisposable
    {
        /// <summary>
        /// Returns the properties of the audio stream (e.g. sample rate, bit rate, number of channels).
        /// </summary>
        AudioProperties Properties { get; }

        /// <summary>
        /// Gets the length of the audio stream in units of bytes.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Gets or sets the current position in the audio stream in units of bytes.
        /// </summary>
        long Position { get; set; }

        /// <summary>
        /// Gets the size of a sample block in bytes. A sample block contains a sample for each channel at
        /// one time instant.
        /// </summary>
        int SampleBlockSize { get; }

        ///// <summary>
        ///// Gets the total length of the audio stream in units of sample blocks.
        ///// </summary>
        //long SampleBlockCount { get; }

        ///// <summary>
        ///// Gets or sets the current position in the audio stream in units of sample blocks.
        ///// </summary>
        //long SampleBlockPosition { get; set; }

        ///// <summary>
        ///// Gets the total length of the audio stream in units of time.
        ///// </summary>
        //TimeSpan TimeLength { get; }

        ///// <summary>
        ///// Gets or sets the current position in the stream in units of time.
        ///// </summary>
        //TimeSpan TimePosition { get; set; }

        /// <summary>
        /// Reads audio byte data from the stream.
        /// The number of bytes to read should be a multiple of SampleBlockSize.
        /// </summary>
        /// <param name="buffer">the target buffer</param>
        /// <param name="offset">the offset in the target buffer</param>
        /// <param name="count">the number of bytes to read</param>
        /// <returns>the number of bytes read, or 0 if the end of the stream has been reached</returns>
        int Read(byte[] buffer, int offset, int count);

        /// <summary>
        /// Closes the stream and releases all associated resources.
        /// </summary>
        void Close();
    }
}
