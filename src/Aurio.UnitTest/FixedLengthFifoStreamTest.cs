using System;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class FixedLengthFifoStreamTest
    {
        [TestMethod]
        public void WriteReadSequenceTestBase()
        {
            WriteReadSequenceTest(
                new FixedLengthFifoStream(new AudioProperties(1, 1, 8, AudioFormat.LPCM), 10)
            );
        }

        [TestMethod]
        public void ReadDelay()
        {
            var s = new FixedLengthFifoStream(new AudioProperties(1, 1, 8, AudioFormat.LPCM), 10);

            Assert.AreEqual(0, s.ReadDelay);

            s.Write(new byte[2], 0, 2);

            Assert.AreEqual(2, s.ReadDelay);

            s.Write(new byte[8], 0, 8);

            Assert.AreEqual(10, s.ReadDelay);

            // Write over capacity
            s.Write(new byte[5], 0, 5);

            Assert.AreEqual(10, s.ReadDelay);
        }

        public static void WriteReadSequenceTest(FixedLengthFifoStream s)
        {
            if (s.Capacity != 10)
            {
                throw new ArgumentException("This test method requires a stream of legnth 10");
            }

            Assert.AreEqual(0, s.WritePosition);
            Assert.AreEqual(0, s.Position);
            // Assert.AreEqual(0, s.Length);
            Assert.AreEqual(10, s.Capacity);

            s.Write(new byte[] { 1 }, 0, 1);

            Assert.AreEqual(1, s.WritePosition);

            // Read 1st byte
            var readBuffer = new byte[1];
            s.Read(readBuffer, 0, 1);

            Assert.AreEqual(1, s.WritePosition);
            Assert.AreEqual(1, s.Position);
            CollectionAssert.AreEqual(new byte[] { 1 }, readBuffer);

            s.Write(new byte[] { 2, 3 }, 0, 2);

            Assert.AreEqual(3, s.WritePosition);
            Assert.AreEqual(1, s.Position);

            // Read 2nd byte
            s.Read(readBuffer, 0, 1);

            Assert.AreEqual(3, s.WritePosition);
            Assert.AreEqual(2, s.Position);
            CollectionAssert.AreEqual(new byte[] { 2 }, readBuffer);

            s.Write(new byte[] { 4, 5 }, 0, 2);

            Assert.AreEqual(5, s.WritePosition);
            Assert.AreEqual(2, s.Position);

            // Read 3rd and 4th byte
            readBuffer = new byte[2];
            s.Read(readBuffer, 0, 2);

            Assert.AreEqual(5, s.WritePosition);
            Assert.AreEqual(4, s.Position);
            CollectionAssert.AreEqual(new byte[] { 3, 4 }, readBuffer);

            // Write 1 byte over the capacity, read index should now move back one byte to position 3
            s.Write(new byte[] { 6, 7, 8, 9, 10, 11 }, 0, 6);

            Assert.AreEqual(10, s.WritePosition);
            Assert.AreEqual(3, s.Position);

            s.Read(readBuffer, 0, 2);

            // Read 5th and 6th byte, read index should now be at position 5
            Assert.AreEqual(10, s.WritePosition);
            Assert.AreEqual(5, s.Position);
            CollectionAssert.AreEqual(new byte[] { 5, 6 }, readBuffer);

            // Buffer is already full and we write 6 more bytes, this should move the read index to -1,
            // which means we lose 1 byte (value 7)
            s.Write(new byte[] { 12, 13, 14, 15, 16, 17 }, 0, 6);

            Assert.AreEqual(10, s.WritePosition);
            Assert.AreEqual(0, s.Position);

            s.Read(readBuffer, 0, 2);

            // Read 8th and 9th byte, read index should now be at position 2
            Assert.AreEqual(10, s.WritePosition);
            Assert.AreEqual(2, s.Position);
            CollectionAssert.AreEqual(new byte[] { 8, 9 }, readBuffer);
        }
    }
}
