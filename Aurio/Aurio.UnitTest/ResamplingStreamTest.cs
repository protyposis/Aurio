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
    public class ResamplingStreamTest {

        private TestContext testContextInstance;
        private ResamplingStream stream;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext) {
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize() {
            stream = new ResamplingStream(
                new NullStream(new AudioProperties(1, 44100, 32, AudioFormat.IEEE), 1000),
                ResamplingQuality.VeryHigh);
        }
        
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod()]
        public void TargetSampleRate() {
            Assert.AreEqual(44100, stream.TargetSampleRate);
            Assert.AreEqual(1, stream.SampleRateRatio);
            stream.TargetSampleRate = 88200;
            Assert.AreEqual(88200, stream.TargetSampleRate);
            Assert.AreEqual(2, stream.SampleRateRatio);
        }

        [TestMethod()]
        public void SampleRateRatio() {
            Assert.AreEqual(44100, stream.TargetSampleRate);
            Assert.AreEqual(1, stream.SampleRateRatio);
            stream.SampleRateRatio = 0.5;
            Assert.AreEqual(22050, stream.TargetSampleRate);
            Assert.AreEqual(0.5, stream.SampleRateRatio);
        }
    }
}
