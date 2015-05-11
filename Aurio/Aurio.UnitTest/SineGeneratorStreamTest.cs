using System;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest {
    [TestClass]
    public class SineGeneratorStreamTest {
        [TestMethod]
        public void CheckLength() {
            int sampleRate = 44100;
            int seconds = 1;
            long expectedLength = sampleRate * seconds * 4; // 4 bytes per 32bit ieee sample

            var sine = new SineGeneratorStream(sampleRate, 440, new TimeSpan(0, 0, seconds));

            Assert.AreEqual(expectedLength, sine.Length);

            long bytesRead = StreamUtil.ReadAllAndCount(sine);
            Assert.AreEqual(expectedLength, bytesRead);
        }

        [TestMethod]
        public void CheckSeek() {
            var sine = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));

            int seek = 100 * 4;

            sine.Position = seek;
            Assert.AreEqual(sine.Position, seek);

            long bytesRead = StreamUtil.ReadAllAndCount(sine);
            Assert.AreEqual(sine.Length - seek, bytesRead);
        }
    }
}
