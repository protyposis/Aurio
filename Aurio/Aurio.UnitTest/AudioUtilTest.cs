using Aurio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Aurio.Streams;

namespace Aurio.UnitTest
{
    
    
    /// <summary>
    ///This is a test class for AudioUtilTest and is intended
    ///to contain all AudioUtilTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AudioUtilTest {


        private TestContext testContextInstance;

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
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for AlignToSamples
        ///</summary>
        [TestMethod()]
        public void AlignToSamplesTest() {
            Interval intervalToAlign = new Interval(1000, 10000);
            AudioProperties audioProperties = new AudioProperties(2, 44100, 32, AudioFormat.IEEE);
            double sampleTicks = AudioUtil.CalculateSampleTicks(audioProperties);
            Interval expected = new Interval((long)(intervalToAlign.From - ((double)intervalToAlign.From % sampleTicks)),
                (long)(intervalToAlign.To + sampleTicks - (intervalToAlign.To % sampleTicks)));
            Interval actual;
            actual = AudioUtil.AlignToSamples(intervalToAlign, audioProperties);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for CalculateSampleTicks
        ///</summary>
        [TestMethod()]
        public void CalculateSampleTicksTest() {
            AudioProperties audioProperties = new AudioProperties(2, 44100, 32, AudioFormat.IEEE);
            double expected = TimeUtil.SECS_TO_TICKS / (double)audioProperties.SampleRate;
            double actual;
            actual = AudioUtil.CalculateSampleTicks(audioProperties);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for CalculateSamples
        ///</summary>
        [TestMethod()]
        public void CalculateSamplesTest() {
            AudioProperties audioProperties = new AudioProperties(2, 44100, 32, AudioFormat.IEEE);
            double sampleTicks = AudioUtil.CalculateSampleTicks(audioProperties);

            for (int x = 0; x < audioProperties.SampleRate * 60; x++) {
                TimeSpan timeSpan = new TimeSpan((long)Math.Ceiling(x * sampleTicks));
                int expected = x;
                int actual;
                actual = AudioUtil.CalculateSamples(audioProperties, timeSpan);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
