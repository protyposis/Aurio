using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Streams {
    /// <summary>
    /// An audio stream that returns no data. Useful for testing purposes.
    /// </summary>
    public class NullStream : IAudioStream {

        private AudioProperties audioProperties;
        private long length;
        private long position;

        public NullStream(AudioProperties audioProperties, long length) {
            this.audioProperties = audioProperties;
            this.length = length;
            this.position = 0;
        }

        #region IAudioStream Members

        public AudioProperties Properties {
            get { return audioProperties; }
        }

        public long Length {
            get { return length; }
        }

        public long Position {
            get { return position; }
            set { position = value; }
        }

        public int SampleBlockSize {
            get { return audioProperties.SampleBlockByteSize; }
        }

        public int Read(byte[] buffer, int offset, int count) {
            int bytesRead;

            if (position + count < length) {
                bytesRead = count;
            }
            else {
                bytesRead = (int)(length - position);
            }

            position += bytesRead;
            return bytesRead;
        }

        #endregion
    }
}
