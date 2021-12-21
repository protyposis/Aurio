using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aurio.Features;
using Aurio.Streams;
using System.Linq;

namespace Aurio.UnitTest
{
    [TestClass]
    public class StreamWindowerTest
    {
        [TestMethod]
        public void TestWindowing()
        {
            float[] samples = new float[] {
                1,1,1,1,1,1,1,1,1,1,
                0,0,0,0,0,0,0,0,0,0,
                -1,-1,-1,-1,-1,-1,-1,-1,-1,-1
            };

            byte[] samplesAsBytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, samplesAsBytes, 0, samplesAsBytes.Length);

            var source = new MemorySourceStream(new System.IO.MemoryStream(samplesAsBytes), new AudioProperties(1, 44100, 32, AudioFormat.IEEE));
            var windower = new StreamWindower(source, 10, 10);
            float[] frame = new float[10];

            // Read first 10 items that are 1
            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(1f, 10).ToArray());

            // Read next 10 items that are 0
            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(0f, 10).ToArray());

            // Read last 10 items that are -1
            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(-1f, 10).ToArray());
        }

        [TestMethod]
        public void TestWindowingWithOverlap50Percent()
        {
            float[] samples = new float[] {
                1,1,1,1,1,1,1,1,1,1,
                0,0,0,0,0,0,0,0,0,0,
                -1,-1,-1,-1,-1,-1,-1,-1,-1,-1
            };

            byte[] samplesAsBytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, samplesAsBytes, 0, samplesAsBytes.Length);

            var source = new MemorySourceStream(new System.IO.MemoryStream(samplesAsBytes), new AudioProperties(1, 44100, 32, AudioFormat.IEEE));
            var windower = new StreamWindower(source, 10, 5);
            float[] frame = new float[10];

            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(1f, 10).ToArray());

            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(1f, 5).Concat(Enumerable.Repeat(0f, 5)).ToArray());

            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(0f, 10).ToArray());

            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(0f, 5).Concat(Enumerable.Repeat(-1f, 5)).ToArray());

            windower.ReadFrame(frame);
            CollectionAssert.AreEqual(frame, Enumerable.Repeat(-1f, 10).ToArray());
        }
    }
}
