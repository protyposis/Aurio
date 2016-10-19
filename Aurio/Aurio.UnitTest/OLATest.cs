using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aurio.Features;
using Aurio.Streams;

namespace Aurio.UnitTest {
    [TestClass]
    public class OLATest {

        [TestMethod]
        public void CreateMonoIEEE() {
            var properties = new AudioProperties(1, 44100, 16, AudioFormat.IEEE);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 500);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateMonoLPCM() {
            var properties = new AudioProperties(1, 44100, 16, AudioFormat.LPCM);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 500);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateStereoIEEE() {
            var properties = new AudioProperties(2, 44100, 16, AudioFormat.IEEE);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 500);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CreateOverlapTooLarge() {
            var properties = new AudioProperties(1, 44100, 16, AudioFormat.IEEE);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 501);
        }

        [TestMethod]
        public void MonoOverlapAdd() {
            var properties = new AudioProperties(1, 44100, 32, AudioFormat.IEEE);
            var targetStream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);


            var sourceStream = new SineGeneratorStream(44100, 440, TimeSpan.FromMinutes(1));

            // 50% overlap with a Hann window is an optimal combination
            int windowSize = 1000;
            int hopSize = 500;
            var window = WindowType.Hann;

            var sw = new StreamWindower(sourceStream, windowSize, hopSize, window);
            var ola = new OLA(targetStream, windowSize, hopSize);

            var frameBuffer = new float[windowSize];
            while(sw.HasNext()) {
                sw.ReadFrame(frameBuffer);
                ola.WriteFrame(frameBuffer);
            }
            ola.Flush();

            Assert.AreEqual(sourceStream.Length, targetStream.Length);
        }
    }
}
