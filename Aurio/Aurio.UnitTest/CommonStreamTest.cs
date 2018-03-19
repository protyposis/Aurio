using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aurio.Streams;
using System.Collections.Generic;
using Aurio.Resampler;

namespace Aurio.UnitTest {
    [TestClass]
    public class CommonStreamTest {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            ResamplerFactory.Factory = new Soxr.ResamplerFactory();
        }

        [TestMethod]
        public void TestReadOverEnd() {
            var streams = GetStreams();
            var buffer = new byte[32768];

            foreach(var stream in streams) {
                long length = stream.Length;
                int frameSize = stream.SampleBlockSize;
                Assert.AreEqual(0, stream.Position);

                // Set position to one frame before end
                stream.Position = length - frameSize;
                Assert.AreEqual(length - frameSize, stream.Position);

                // Force read over the end of the stream
                int bytesRead = StreamUtil.ForceRead(stream, buffer, 0, buffer.Length);
                Assert.AreEqual(frameSize, bytesRead);
                Assert.AreEqual(length, stream.Position);
            }
        }

        private List<IAudioStream> GetStreams() {
            return new List<IAudioStream> {
                new BufferedStream(GetSourceStream(), 1024, true),
                new BufferedStream(GetSourceStream(), 1024, false),
                new CropStream(GetSourceStream()),
                new IeeeStream(GetSourceStream()),
                new MonoStream(GetSourceStream()),
                new NullStream(GetSourceStream().Properties, GetSourceStream().Length),
                new OffsetStream(GetSourceStream()),
                new PhaseInversionStream(GetSourceStream()),
                new ResamplingStream(GetSourceStream(), ResamplingQuality.Medium),
                new ResamplingStream(GetSourceStream(), ResamplingQuality.Medium, 96000),
                new ResamplingStream(GetSourceStream(), ResamplingQuality.Medium, 11025),
                GetSourceStream(), // new SineGeneratorStream()
                new TimeWarpStream(GetSourceStream()),
                new TolerantStream(GetSourceStream()),
                new VolumeClipStream(GetSourceStream()),
                new VolumeControlStream(GetSourceStream()),
                new VolumeMeteringStream(GetSourceStream()),

                // Special cases
                new TolerantStream(new BufferedStream(new TimeWarpStream(new IeeeStream(new NullStream(new AudioProperties(2, 44100, 16, AudioFormat.LPCM), GetSourceStream().Length))), 1024 * 256 * 4, true)),
        };
        }

        private IAudioStream GetSourceStream() {
            return new SineGeneratorStream(44100, 440, new TimeSpan(0, 1, 0));
        }
    }
}
