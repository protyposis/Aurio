using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Streams {
    public interface IAudioWriterStream : IAudioStream {
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
