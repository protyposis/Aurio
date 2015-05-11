using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Streams {

    /// <summary>
    /// A stream sourced from a <see cref="System.IO.MemoryStream"/>, which can also wrap a raw byte buffer.
    /// </summary>
    public class MemorySourceStream : IAudioStream {

        private MemoryStream source;
        private AudioProperties properties;

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
    }
}
