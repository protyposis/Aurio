using System.IO;
using Xunit;

namespace Aurio.FFmpeg.UnitTest
{
    public class AudioDecode
    {
        [Fact]
        public void Mp3ReadProperties()
        {
            var s = new FFmpegSourceStream(
                new FileInfo("./Resources/sine440-44100-16-mono-200ms.mp3")
            );

            Assert.Equal(1, s.Properties.Channels);
            Assert.Equal(44100, s.Properties.SampleRate);
        }

        [Fact]
        public void Mp3ReadData()
        {
            var s = new FFmpegSourceStream(
                new FileInfo("./Resources/sine440-44100-16-mono-200ms.mp3")
            );

            var length = StreamUtil.ReadAllAndCount(s);
            Assert.True(length > 46000);
        }
    }
}
