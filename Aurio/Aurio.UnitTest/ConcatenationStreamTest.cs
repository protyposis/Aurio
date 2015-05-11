using System;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest {
    [TestClass]
    public class ConcatenationStreamTest {
        [TestMethod]
        public void CheckSingleSourceLength() {
            var stream = new SineGeneratorStream(44100, 440, new TimeSpan(0,0,1));
            var cc = new ConcatenationStream(stream);

            // Ensure that the lengths match
            Assert.AreEqual(stream.Length, cc.Length);

            var bytesRead = StreamUtil.ReadAllAndCount(cc);

            // Ensure that the read bytes equals the length
            Assert.AreEqual(stream.Length, bytesRead);
        }

        [TestMethod]
        public void CheckSingleSourceSeeking() {
            var stream = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var cc = new ConcatenationStream(stream);

            int seek = 200 * stream.SampleBlockSize;
            cc.Position = seek;

            // Ensure that the position is correct
            Assert.AreEqual(seek, cc.Position);

            var bytesRead = StreamUtil.ReadAllAndCount(cc);

            // Ensure that the expected remaining bytes match the seek position
            Assert.AreEqual(cc.Length - seek, bytesRead);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "format mismatch not detected")]
        public void CheckMultiSourceMatchingFormat() {
            var stream1 = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var stream2 = new SineGeneratorStream(96000, 440, new TimeSpan(0, 0, 1));
            var cc = new ConcatenationStream(stream1, stream2);
        }

        [TestMethod]
        public void CheckMultiSourceLength() {
            var stream1 = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var stream2 = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var cc = new ConcatenationStream(stream1, stream2);

            // Ensure that the lengths match
            Assert.AreEqual(stream1.Length + stream2.Length, cc.Length);

            var bytesRead = StreamUtil.ReadAllAndCount(cc);

            // Ensure that the read bytes equals the length
            Assert.AreEqual(stream1.Length + stream2.Length, bytesRead);
        }

        [TestMethod]
        public void CheckMultiSourceRandomSeeking() {
            var stream1 = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var stream2 = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var cc = new ConcatenationStream(stream1, stream2);

            int seekOffset = 200 * stream1.SampleBlockSize;

            // Check seek into first segment
            cc.Position = seekOffset;
            Assert.AreEqual(seekOffset, cc.Position);
            var bytesRead = StreamUtil.ReadAllAndCount(cc);
            Assert.AreEqual(cc.Length - seekOffset, bytesRead);

            // Check seek into second segment
            cc.Position = stream1.Length + seekOffset;
            Assert.AreEqual(stream1.Length + seekOffset, cc.Position);
            bytesRead = StreamUtil.ReadAllAndCount(cc);
            Assert.AreEqual(cc.Length - stream1.Length - seekOffset, bytesRead);

            // Check seek back into first segment
            cc.Position = seekOffset;
            Assert.AreEqual(seekOffset, cc.Position);
            bytesRead = StreamUtil.ReadAllAndCount(cc);
            Assert.AreEqual(cc.Length - seekOffset, bytesRead);
        }

        [TestMethod]
        public void CheckMultiSourceSeekToEnd() {
            var stream1 = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var stream2 = new SineGeneratorStream(44100, 440, new TimeSpan(0, 0, 1));
            var cc = new ConcatenationStream(stream1, stream2);

            // Seek to the end where zero bytes are expected to be read
            cc.Position = cc.Length;
            var bytesRead = StreamUtil.ReadAllAndCount(cc);
            Assert.AreEqual(0, bytesRead);
        }
    }
}
