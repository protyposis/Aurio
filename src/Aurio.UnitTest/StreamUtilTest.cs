using System;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class StreamUtilTest
    {
        [TestMethod]
        public void ReadBytes()
        {
            var sampleCount = 50;
            var s = new SineGeneratorStream(sampleCount, 440, TimeSpan.FromSeconds(1));

            var samples = StreamUtil.ReadBytes(s, sampleCount);

            Assert.AreEqual(sampleCount, samples.Length / s.SampleBlockSize);
        }

        [TestMethod]
        public void ReadFloats()
        {
            var sampleCount = 50;
            var s = new SineGeneratorStream(sampleCount, 440, TimeSpan.FromSeconds(1));

            var samples = StreamUtil.ReadFloats(s, sampleCount);

            Assert.AreEqual(sampleCount, samples.Length);
        }
    }
}
