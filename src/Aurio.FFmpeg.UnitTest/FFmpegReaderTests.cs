using System;
using System.IO;
using Xunit;

namespace Aurio.FFmpeg.UnitTest
{
    public class FFmpegReaderTests
    {
        /// <summary>
        /// MKV does not carry individual stream lenghts, only a total container length.
        /// </summary>
        [Fact]
        public void MKV_LengthFromContainerAvailable()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.mkv");

            var reader = new FFmpegReader(fileInfo, Type.Audio);

            Assert.NotEqual(long.MinValue, reader.AudioOutputConfig.length);
        }

        [Fact]
        public void TS_NonZeroStartTime()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var reader = new FFmpegReader(fileInfo, Type.Audio);
            var sourceBuffer = new byte[reader.FrameBufferSize];
            var startTimeSecs = 2000;

            reader.ReadFrame(out long readerPosition, sourceBuffer, sourceBuffer.Length, out _);

            Assert.Equal(
                startTimeSecs * reader.AudioOutputConfig.format.sample_rate,
                readerPosition
            );
        }

        [Fact]
        public void SignalEOF()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var reader = new FFmpegReader(fileInfo, Type.Audio);
            var sourceBuffer = new byte[reader.FrameBufferSize];
            int result;

            // Read over all frames (seeking does not work here due to short stream)
            do
            {
                result = reader.ReadFrame(out _, sourceBuffer, sourceBuffer.Length, out _);
            } while (result > 0);

            // -1 signals EOF
            Assert.Equal(-1, result);
        }

        [Fact]
        public void Utf8FileName()
        {
            var fileInfo = new FileInfo("./Resources/utf8 special ° character.wav");

            var reader = new FFmpegReader(fileInfo, Type.Audio);
        }
    }
}
