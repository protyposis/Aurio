using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Streams {
    /// <summary>
    /// An audio stream.
    /// </summary>
    public interface IAudioStream {
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
    }
}
