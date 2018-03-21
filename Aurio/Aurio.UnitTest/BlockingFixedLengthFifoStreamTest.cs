using System;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class BlockingFixedLengthFifoStreamTest
    {
        [TestMethod]
        public void WriteReadSequenceTest()
        {
            var s = new BlockingFixedLengthFifoStream(new AudioProperties(1, 1, 8, AudioFormat.LPCM), 10);

            Assert.AreEqual(0, s.Position);
            Assert.AreEqual(0, s.ReadPosition);
            Assert.AreEqual(0, s.Length);
            Assert.AreEqual(10, s.Capacity);

            s.Write(new byte[] { 1 }, 0, 1);

            Assert.AreEqual(1, s.Position);

            // Read 1st byte
            var readBuffer = new byte[1];
            s.Read(readBuffer, 0, 1);

            Assert.AreEqual(1, s.Position);
            Assert.AreEqual(1, s.ReadPosition);
            CollectionAssert.AreEqual(new byte[] { 1 }, readBuffer);

            s.Write(new byte[] { 2, 3 }, 0, 2);

            Assert.AreEqual(3, s.Position);
            Assert.AreEqual(1, s.ReadPosition);

            // Read 2nd byte
            s.Read(readBuffer, 0, 1);

            Assert.AreEqual(3, s.Position);
            Assert.AreEqual(2, s.ReadPosition);
            CollectionAssert.AreEqual(new byte[] { 2 }, readBuffer);

            s.Write(new byte[] { 4, 5 }, 0, 2);

            Assert.AreEqual(5, s.Position);
            Assert.AreEqual(2, s.ReadPosition);

            // Read 3rd and 4th byte
            readBuffer = new byte[2];
            s.Read(readBuffer, 0, 2);

            Assert.AreEqual(5, s.Position);
            Assert.AreEqual(4, s.ReadPosition);
            CollectionAssert.AreEqual(new byte[] { 3, 4 }, readBuffer);

            // Write 1 byte over the capacity, read index should now move back one byte to position 3
            s.Write(new byte[] { 6, 7, 8, 9, 10, 11 }, 0, 6);

            Assert.AreEqual(10, s.Position);
            Assert.AreEqual(3, s.ReadPosition);

            s.Read(readBuffer, 0, 2);

            // Read 5th and 6th byte, read index should now be at position 5
            Assert.AreEqual(10, s.Position);
            Assert.AreEqual(5, s.ReadPosition);
            CollectionAssert.AreEqual(new byte[] { 5, 6 }, readBuffer);

            // Buffer is already full and we write 6 more bytes, this should move the read index to -1,
            // which means we lose 1 byte (value 7)
            s.Write(new byte[] { 12, 13, 14, 15, 16, 17 }, 0, 6);

            Assert.AreEqual(10, s.Position);
            Assert.AreEqual(0, s.ReadPosition);

            s.Read(readBuffer, 0, 2);

            // Read 8th and 9th byte, read index should now be at position 2
            Assert.AreEqual(10, s.Position);
            Assert.AreEqual(2, s.ReadPosition);
            CollectionAssert.AreEqual(new byte[] { 8, 9 }, readBuffer);
        }
    }
}
