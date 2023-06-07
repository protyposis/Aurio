using Aurio.Resampler;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Aurio.UnitTest
{
    /// <summary>
    ///This is a test class for ResamplingStreamTest and is intended
    ///to contain all ResamplingStreamTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ResamplingStreamTest
    {
        private TestContext testContextInstance;
        private ResamplingStream stream;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            ResamplerFactory.Factory = new Soxr.ResamplerFactory();
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            stream = new ResamplingStream(
                new NullStream(new AudioProperties(1, 44100, 32, AudioFormat.IEEE), 1000),
                ResamplingQuality.VeryHigh
            );
        }

        [TestMethod()]
        public void TargetSampleRate()
        {
            Assert.AreEqual(44100, stream.TargetSampleRate);
            Assert.AreEqual(1, stream.SampleRateRatio);
            stream.TargetSampleRate = 88200;
            Assert.AreEqual(88200, stream.TargetSampleRate);
            Assert.AreEqual(2, stream.SampleRateRatio);
        }

        [TestMethod()]
        public void SampleRateRatio()
        {
            Assert.AreEqual(44100, stream.TargetSampleRate);
            Assert.AreEqual(1, stream.SampleRateRatio);
            stream.SampleRateRatio = 0.5;
            Assert.AreEqual(22050, stream.TargetSampleRate);
            Assert.AreEqual(0.5, stream.SampleRateRatio);
        }
    }
}
