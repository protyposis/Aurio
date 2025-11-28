using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Xunit;

namespace Aurio.FFmpeg.UnitTest
{
    public class FFmpegSourceStreamTests
    {
        [Fact]
        public void CreateWaveProxy_ProxyFileInfoIsNull()
        {
            FileInfo? proxyFileInfo = null;

            void act() => FFmpegSourceStream.CreateWaveProxy(new MemoryStream(), proxyFileInfo);

            Assert.Throws<ArgumentNullException>(act);
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithoutDirectory_Windows()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var fileDescriptor = new FFmpegSourceStream.FileDescriptor(
                "X:\\folder\\file.wav",
                0,
                DateTime.Now
            );

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(fileDescriptor);

            Assert.Equal("X:\\folder\\file.wav.ffproxy.wav", proxyFileInfo.FullName);
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithoutDirectory_Unix()
        {
            Skip.If(OperatingSystem.IsWindows());

            var fileDescriptor = new FFmpegSourceStream.FileDescriptor(
                "/folder/file.wav",
                0,
                DateTime.Now
            );

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(fileDescriptor);

            Assert.Equal("/folder/file.wav.ffproxy.wav", proxyFileInfo.FullName);
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithDirectory_Windows()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var fileDescriptor = new FFmpegSourceStream.FileDescriptor(
                "X:\\folder\\file.wav",
                1,
                new DateTime(0).AddMilliseconds(100)
            );
            var directoryInfo = new DirectoryInfo("Y:\\temp\\dir");

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(
                fileDescriptor,
                directoryInfo
            );

            Assert.Equal(
                "Y:\\temp\\dir\\c80fb33f795ec563becb32d569268c831cfe8bbb360b00eb0da10c260b7870b6.ffproxy.wav",
                proxyFileInfo.FullName
            );
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithDirectory_Unix()
        {
            Skip.If(OperatingSystem.IsWindows());

            var fileDescriptor = new FFmpegSourceStream.FileDescriptor(
                "/folder/file.wav",
                1,
                new DateTime(0).AddMilliseconds(100)
            );
            var directoryInfo = new DirectoryInfo("/temp/dir");

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(
                fileDescriptor,
                directoryInfo
            );

            Assert.Equal(
                "/temp/dir/54f862953943df20eb4b2c8b343b118ab7134f1b5980b6dd261d452e33569afb.ffproxy.wav",
                proxyFileInfo.FullName
            );
        }

        [Fact]
        public void NoStreamDuration_SeekingSupported()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.mkv");

            var act = () => new FFmpegSourceStream(fileInfo);
            var ex = Record.Exception(act);

            // Assert no FileNotSeekableException being thrown
            Assert.Null(ex);
        }

        [Fact]
        public void NonZeroStartTime_SeekingSupported()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var s = new FFmpegSourceStream(fileInfo);

            s.Position = 1000;

            Assert.Equal(1000, s.Position);
        }

        [Fact]
        public void NonZeroStartTime_InitialPositionIsZero()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var s = new FFmpegSourceStream(fileInfo);

            Assert.Equal(0, s.Position);
        }

        [Fact]
        public void ReadTwoFrames_UpdatePosition()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var s = new FFmpegSourceStream(fileInfo);
            var frameSize = 1152 * s.SampleBlockSize;
            var buffer = new byte[frameSize];

            var bytesRead = s.Read(buffer, 0, buffer.Length);

            Assert.Equal(frameSize, s.Position);
            Assert.Equal(frameSize, bytesRead);

            bytesRead = s.Read(buffer, 0, buffer.Length);

            Assert.Equal(frameSize * 2, s.Position);
            Assert.Equal(frameSize, bytesRead);
        }

        [Fact]
        public void ReadIncompleteFrames_UpdatePosition()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var s = new FFmpegSourceStream(fileInfo);
            var frameSize = 1152 * s.SampleBlockSize;
            var buffer = new byte[frameSize];

            // Read half frame
            var bytesRead = s.Read(buffer, 0, buffer.Length / 2);

            Assert.Equal(frameSize / 2, s.Position);
            Assert.Equal(frameSize / 2, bytesRead);

            // Read one sample
            bytesRead = s.Read(buffer, 0, s.SampleBlockSize);

            Assert.Equal(frameSize / 2 + s.SampleBlockSize, s.Position);
            Assert.Equal(s.SampleBlockSize, bytesRead);
        }

        [Fact]
        public void ThrowWhenFirstPtsCannotBeDetermined()
        {
            var readerMock = new Mock<FFmpegReader>(
                new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts"),
                Type.Audio
            );
            readerMock
                .Setup(
                    m =>
                        m.ReadFrame(
                            out It.Ref<long>.IsAny,
                            It.IsAny<byte[]>(),
                            It.IsAny<int>(),
                            out It.Ref<Type>.IsAny
                        )
                )
                .Returns(-1);

            var act = () => new FFmpegSourceStream(readerMock.Object);

            Assert.Throws<FFmpegSourceStream.FileNotSeekableException>(act);
            readerMock.Verify(
                m =>
                    m.ReadFrame(
                        out It.Ref<long>.IsAny,
                        It.IsAny<byte[]>(),
                        It.IsAny<int>(),
                        out It.Ref<Type>.IsAny
                    ),
                Times.Once()
            );
        }

        [Fact]
        public void SeekBeyondTarget_ReseekToPreviousFrame()
        {
            var readerMock = new Mock<FFmpegReader>(
                new FileInfo("./Resources/sine440-44100-16-mono-200ms.mkv"),
                Type.Audio
            );
            var timestamps = new Queue<long>(
                new long[]
                {
                    // First read determines PTS offset.
                    0,
                    // Second read expects frame to contain sample 25000 (100000 / sample block size),
                    // so return 25001 to simulate that the next frame was read instead.
                    25001,
                    // After the expected seek and re-read, indicate that frame contains expected sample.
                    25000
                }
            );
            readerMock
                .Setup(
                    m =>
                        m.ReadFrame(
                            out It.Ref<long>.IsAny,
                            It.IsAny<byte[]>(),
                            It.IsAny<int>(),
                            out It.Ref<Type>.IsAny
                        )
                )
                .Callback(
                    (out long timestamp, byte[] buffer, int bufferSize, out Type type) =>
                    {
                        timestamp = timestamps.Dequeue();
                        type = Type.Audio;
                    }
                )
                .Returns(1);
            var s = new FFmpegSourceStream(readerMock.Object);

            s.Position = 100000;

            readerMock.Verify(
                m =>
                    m.ReadFrame(
                        out It.Ref<long>.IsAny,
                        It.IsAny<byte[]>(),
                        It.IsAny<int>(),
                        out It.Ref<Type>.IsAny
                    ),
                Times.Exactly(3)
            );
        }

        [Fact]
        public void SeekBeyondEnd_PositionCanBeSet()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var s = new FFmpegSourceStream(fileInfo);
            var position = 10000000000 * s.SampleBlockSize;

            s.Position = position;

            Assert.Equal(position, s.Position);
        }

        [Fact]
        public void SeekBeyondEnd_ReadIndicatesEndOfStream()
        {
            var fileInfo = new FileInfo("./Resources/sine440-44100-16-mono-200ms.ts");
            var s = new FFmpegSourceStream(fileInfo);
            var position = 10000000000 * s.SampleBlockSize;

            s.Position = position;
            var bytesRead = s.Read(new byte[1000], 0, 1000);

            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void SuggestWaveProxyFileName_FileWriteTimeChange()
        {
            string path = "/virtual/test.bin";
            long length = 1234;
            DateTime t1 = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            DateTime t2 = t1.AddMilliseconds(1); // changed timestamp

            var name1 = FFmpegSourceStream.SuggestWaveProxyFileName(
                new FFmpegSourceStream.FileDescriptor(path, length, t1)
            );
            var name2 = FFmpegSourceStream.SuggestWaveProxyFileName(
                new FFmpegSourceStream.FileDescriptor(path, length, t2)
            );

            Assert.NotEqual(name1, name2);
        }
    }
}
