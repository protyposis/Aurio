using Aurio.Audio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Aurio.Audio.UnitTest
{
    
    
    /// <summary>
    ///This is a test class for MonoStreamTest and is intended
    ///to contain all MonoStreamTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MonoStreamTest {


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

        [TestMethod()]
        public void StereoToMono() {
            IAudioStream sourceStream;
            MonoStream monoStream;
            CreateStream(2, 1, 100, out sourceStream, out monoStream);

            Assert.AreEqual(monoStream.Length, (double)sourceStream.Length / 2);
        }

        [TestMethod()]
        public void QuadroToMono() {
            IAudioStream sourceStream;
            MonoStream monoStream;
            CreateStream(4, 1, 100, out sourceStream, out monoStream);

            Assert.AreEqual(monoStream.Length, (double)sourceStream.Length / 4);
        }

        [TestMethod()]
        public void QuadroToStereo() {
            IAudioStream sourceStream;
            MonoStream monoStream;
            CreateStream(4, 2, 100, out sourceStream, out monoStream);

            Assert.AreEqual(monoStream.Length, (double)sourceStream.Length / 2);
        }

        [TestMethod()]
        public void MonoToMono() {
            IAudioStream sourceStream;
            MonoStream monoStream;
            CreateStream(1, 1, 100, out sourceStream, out monoStream);

            Assert.AreEqual(monoStream.Length, (double)sourceStream.Length);
        }

        [TestMethod()]
        public void MonoToStereo() {
            IAudioStream sourceStream;
            MonoStream monoStream;
            CreateStream(1, 2, 100, out sourceStream, out monoStream);

            Assert.AreEqual(monoStream.Length, (double)sourceStream.Length * 2);
        }

        [TestMethod()]
        public void MonoToQuadro() {
            IAudioStream sourceStream;
            MonoStream monoStream;
            CreateStream(1, 4, 100, out sourceStream, out monoStream);

            Assert.AreEqual(monoStream.Length, (double)sourceStream.Length * 4);
        }

        private void CreateStream(int inputChannels, int outputChannels, int lengthInSeconds, 
            out IAudioStream sourceStream, out MonoStream monoStream) {
            sourceStream = new NullStream(
                    new AudioProperties(inputChannels, 44100, 32, AudioFormat.IEEE), 
                    44100 * inputChannels * 4 /*bytesPerSample*/ * lengthInSeconds);
            monoStream = new MonoStream(sourceStream, outputChannels);
        }
    }
}
