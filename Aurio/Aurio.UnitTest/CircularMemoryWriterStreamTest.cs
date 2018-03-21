using System;
using System.IO;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class CircularMemoryWriterStreamTest
    {
        private readonly AudioProperties _properties = new AudioProperties(1, 1, 8, AudioFormat.LPCM); // sampleBlockSize is 1;

        [TestMethod]
        public void InitializeWithCapacity()
        {
            var s = new CircularMemoryWriterStream(_properties, 10);

            Assert.AreEqual(10, s.Length);
            Assert.AreEqual(0, s.Position);
        }

        [TestMethod]
        public void InitializeWithMemoryStream()
        {
            var s = new CircularMemoryWriterStream(_properties, new MemoryStream(new byte[10]));

            Assert.AreEqual(10, s.Length);
            Assert.AreEqual(0, s.Position);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InitializeWithMemoryStreamWithoutFixedCapacity()
        {
            var s = new CircularMemoryWriterStream(_properties, new MemoryStream());
        }

        [TestMethod]
        public void WriteBelowCapacity()
        {
            var b = new byte[10];
            var m = new MemoryStream(b);
            var s = new CircularMemoryWriterStream(_properties, m);

            s.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

            Assert.AreEqual(10, s.Length);
            Assert.AreEqual(5, s.Position);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 0, 0, 0, 0, 0 }, b);
        }

        [TestMethod]
        public void WriteBelowCapacityAtPositionWithoutWrap()
        {
            var b = new byte[10];
            var m = new MemoryStream(b);
            var s = new CircularMemoryWriterStream(_properties, m);

            s.Position = 1;
            s.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

            Assert.AreEqual(10, s.Length);
            Assert.AreEqual(6, s.Position);

            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5, 0, 0, 0, 0 }, b);
        }

        [TestMethod]
        public void WriteBelowCapacityAtPositionWithWrap()
        {
            var b = new byte[10];
            var m = new MemoryStream(b);
            var s = new CircularMemoryWriterStream(_properties, m);

            s.Position = 9;
            s.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

            Assert.AreEqual(10, s.Length);
            Assert.AreEqual(10, s.Position);

            CollectionAssert.AreEqual(new byte[] { 2, 3, 4, 5, 0, 0, 0, 0, 0, 1 }, b);
        }

        [TestMethod]
        public void WriteAboveCapacity()
        {
            var b = new byte[10];
            var m = new MemoryStream(b);
            var s = new CircularMemoryWriterStream(_properties, m);

            s.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, 0, 15);

            Assert.AreEqual(10, s.Length);
            Assert.AreEqual(10, s.Position);

            CollectionAssert.AreEqual(new byte[] { 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, b);
        }

        [TestMethod]
        public void ReadBelowCapacity()
        {
            var s = new CircularMemoryWriterStream(_properties, 10);
            s.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0, 10);
            s.Position = 0;

            var readBuffer = new byte[5];
            var bytesRead = s.Read(readBuffer, 0, 5);

            Assert.AreEqual(5, s.Position);
            Assert.AreEqual(5, bytesRead);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, readBuffer);
        }

        [TestMethod]
        public void ReadBelowCapacityAtPositionWithoutWrap()
        {
            var s = new CircularMemoryWriterStream(_properties, 10);
            s.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0, 10);
            s.Position = 0;

            s.Position = 1;
            var readBuffer = new byte[5];
            var bytesRead = s.Read(readBuffer, 0, 5);

            Assert.AreEqual(6, s.Position);
            Assert.AreEqual(5, bytesRead);

            CollectionAssert.AreEqual(new byte[] { 2, 3, 4, 5, 6 }, readBuffer);
        }

        [TestMethod]
        public void ReadAboveCapacity()
        {
            var s = new CircularMemoryWriterStream(_properties, 10);
            s.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0, 10);
            s.Position = 0;

            var readBuffer = new byte[15];
            var bytesRead = s.Read(readBuffer, 0, 15);

            Assert.AreEqual(10, s.Position);
            Assert.AreEqual(10, bytesRead);
        }

        [TestMethod]
        public void ReadAboveCapacityAtPositionWithoutWrap()
        {
            var s = new CircularMemoryWriterStream(_properties, 10);
            s.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0, 10);
            s.Position = 0;

            s.Position = 9;
            var readBuffer = new byte[5];
            var bytesRead = s.Read(readBuffer, 0, 5);

            Assert.AreEqual(10, s.Position);
            Assert.AreEqual(1, bytesRead);

            CollectionAssert.AreEqual(new byte[] { 10, 0, 0, 0, 0 }, readBuffer);
        }

        [TestMethod]
        public void ReadAboveCapacityAtPositionWithWrap()
        {
            var b = new byte[10];
            var m = new MemoryStream(b);
            var s = new CircularMemoryWriterStream(_properties, m);

            s.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 0, 9);
            s.Write(new byte[] { 10, 11 }, 0, 2);

            s.Position = 0;
            var readBuffer = new byte[1];
            s.Read(readBuffer, 0, 1);

            CollectionAssert.AreEqual(new byte[] { 2 }, readBuffer);
        }

        [TestMethod]
        public void SimpleWriteRead()
        {
            var b = new byte[10];
            var m = new MemoryStream(b);
            var s = new CircularMemoryWriterStream(_properties, m);

            s.Write(new byte[] { 1 }, 0, 1);

            s.Position = 0;
            var readBuffer = new byte[1];
            s.Read(readBuffer, 0, 1);

            CollectionAssert.AreEqual(new byte[] { 1 }, readBuffer);
        }

        [TestMethod]
        public void ReadOnlyAsMuchAsWritten()
        {
            var s = new CircularMemoryWriterStream(_properties, 10);

            s.Write(new byte[] { 1 }, 0, 1);

            s.Position = 0;
            var readBuffer = new byte[5];
            var bytesRead = s.Read(readBuffer, 0, 5);

            // make sure that the stream does not read more data as has been written, 
            // i.e. in this case there is only one byte to read
            Assert.AreEqual(1, bytesRead);
        }
    }
}
