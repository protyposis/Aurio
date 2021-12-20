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
        public DummyAudioTrack(string name, TimeSpan length): base(new AudioProperties(1, 44100, 32, AudioFormat.IEEE))
        {
            Name = name;
            Length = length;
        }

        public override IAudioStream CreateAudioStream(bool warp = true)
        {
            IAudioStream stream = new SineGeneratorStream(SourceProperties.SampleRate, 0f, Length);

            if (warp)
            {
                return new TimeWarpStream(stream, TimeWarps);
            }

            return stream;
        }
    }
}
