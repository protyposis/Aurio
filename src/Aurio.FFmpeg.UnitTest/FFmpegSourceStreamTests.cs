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

        [Fact]
        public void SuggestWaveProxyFileInfo_WithoutDirectory()
        {
            var fileInfo = new FileInfo("X:\\folder\\file.wav");

            var proxyFileInfo = FFmpegSourceStream.SuggestWaveProxyFileInfo(fileInfo);

            Assert.Equal("X:\\folder\\file.wav.ffproxy.wav", proxyFileInfo.FullName);
        }

        [Fact]
        public void SuggestWaveProxyFileInfo_WithDirectory()
        {
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
    }
}
