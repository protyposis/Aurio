using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    /// <summary>
    /// Stream that avoids exceptions, e.g. when a position beyond the stream length is set.
    /// </summary>
    public class TolerantStream : AbstractAudioStreamWrapper {
        
        public TolerantStream(IAudioStream sourceStream) : base(sourceStream) { }

        public override long Position {
            set { sourceStream.Position = value > sourceStream.Length ? sourceStream.Length : value; }
        }
    }
}
