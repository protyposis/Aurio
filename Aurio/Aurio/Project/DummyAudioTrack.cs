using System;
using System.Collections.Generic;
using System.Text;
using Aurio.Streams;

namespace Aurio.Project
{
    /// <summary>
    /// An audio track that does not have a real backing media file. Can be used as a placeholder for a real audio track.
    /// </summary>
    public class DummyAudioTrack : AudioTrack
    {
        public DummyAudioTrack(string name, TimeSpan length)
        {
            Name = name;
            Length = length;
        }
    }
}
