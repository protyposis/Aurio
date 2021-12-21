using Aurio.Streams;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Aurio.UnitTest
{
    [TestClass]
    public class MemoryWriterStreamTest
    {
        [TestMethod]
        public void Write()
        {
            var stream = new MemoryStream();
            var properties = new AudioProperties(2, 44100, 16, AudioFormat.IEEE);
            var writer = new MemoryWriterStream(stream, properties);

            Assert.AreEqual(0, writer.Length);

            int size = writer.SampleBlockSize * 1000;
            writer.Write(new byte[size], 0, size);

            Assert.AreEqual(size, writer.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "block alignment must be invalid")]
        public void WriteInvalidAligment()
        {
            var stream = new MemoryStream();
            var properties = new AudioProperties(2, 44100, 16, AudioFormat.IEEE);
            var writer = new MemoryWriterStream(stream, properties);

            Assert.AreEqual(0, writer.Length);

            int size = writer.SampleBlockSize * 1000 + 1;
            writer.Write(new byte[size], 0, size);

            Assert.AreEqual(size, writer.Length);
        }
    }
}
