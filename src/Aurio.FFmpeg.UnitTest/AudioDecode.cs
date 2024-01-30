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
            var expectedMinLength = 44100 * 0.2 * 4; // Fs * 200ms * sampleSize (min, because MP3 adds additional samples)

            var length = StreamUtil.ReadAllAndCount(s);
            Assert.True(length >= expectedMinLength);
        }

        [Fact]
        public void TS_ReadDataUntilEnd()
        {
            var s = new FFmpegSourceStream(
                new FileInfo("./Resources/sine440-44100-16-mono-200ms.mp3")
            );

            StreamUtil.ReadAllAndCount(s);

            // Test succeeds when reading does not get stuck in infinite loop at EOF
        }
    }
}
