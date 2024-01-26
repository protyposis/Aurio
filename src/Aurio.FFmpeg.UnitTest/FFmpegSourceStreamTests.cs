using System;
using System.IO;
using Xunit;

namespace Aurio.FFmpeg.UnitTest
{
    public class FFmpegSourceStreamTests
    {
        [Fact]
        public void CreateWaveProxy_ProxyFileInfoIsNull()
        {
            FileInfo fileInfo = new("X:\\test.file");
            FileInfo? proxyFileInfo = null;

            void act() => FFmpegSourceStream.CreateWaveProxy(fileInfo, proxyFileInfo);

            Assert.Throws<ArgumentNullException>(act);
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithoutDirectory_Windows()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var fileInfo = new FileInfo("X:\\folder\\file.wav");

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(fileInfo);

            Assert.Equal("X:\\folder\\file.wav.ffproxy.wav", proxyFileInfo.FullName);
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithoutDirectory_Unix()
        {
            Skip.If(OperatingSystem.IsWindows());

            var fileInfo = new FileInfo("/folder/file.wav");

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(fileInfo);

            Assert.Equal("/folder/file.wav.ffproxy.wav", proxyFileInfo.FullName);
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithDirectory_Windows()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            var fileInfo = new FileInfo("X:\\folder\\file.wav");
            var directoryInfo = new DirectoryInfo("Y:\\temp\\dir");

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(
                fileInfo,
                directoryInfo
            );

            Assert.Equal(
                "Y:\\temp\\dir\\87c18cf1c8d8e07552df4ecc1ef629995fca9c59ad47ccf7eb4816de33590af7.ffproxy.wav",
                proxyFileInfo.FullName
            );
        }

        [SkippableFact]
        public void SuggestWaveProxyFileInfo_WithDirectory_Unix()
        {
            Skip.If(OperatingSystem.IsWindows());

            var fileInfo = new FileInfo("/folder/file.wav");
            var directoryInfo = new DirectoryInfo("/temp/dir");

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(
                fileInfo,
                directoryInfo
            );

            Assert.Equal(
                "/temp/dir/e2c5b9282d156c5bdbf95639ca2ce2516b096e4828b7aa16620cb317cc90b3d9.ffproxy.wav",
                proxyFileInfo.FullName
            );
        }
    }
}
