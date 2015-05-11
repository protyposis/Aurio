using AudioAlign.Audio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AudioAlign.Audio.UnitTest {


    /// <summary>
    ///This is a test class for DynamicResamplingStreamTest and is intended
    ///to contain all DynamicResamplingStreamTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TimeWarpStreamTest {


        private TestContext testContextInstance;
        private static TimeWarpStream stream;

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
        //    stream = new TimeWarpStream(
        //        new NullStream(new AudioProperties(1, 44100, 32, AudioFormat.IEEE), 70),
        //        ResamplingQuality.SincBest);
        //    stream.Mappings.Add(new TimeWarp { From = 10, To = 15 });
        //    stream.Mappings.Add(new TimeWarp { From = 20, To = 30 });
        //    stream.Mappings.Add(new TimeWarp { From = 30, To = 45 });
        //    stream.Mappings.Add(new TimeWarp { From = 40, To = 48 });
        //    stream.Mappings.Add(new TimeWarp { From = 50, To = 50 });
        //    stream.Mappings.Add(new TimeWarp { From = 60, To = 55 });
        //    stream.Mappings.Add(new TimeWarp { From = 65, To = 60 });
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

        //[TestMethod()]
        //public void Mapping01() {
        //    Assert.AreEqual(7, stream.CalculateWarpedPosition(5));
        //    //Assert.AreEqual(5, stream.CalculateSourcePosition(7));
        //}

        //[TestMethod()]
        //public void Mapping02() {
        //    Assert.AreEqual(37, stream.CalculateWarpedPosition(25));
        //    //Assert.AreEqual(25, stream.CalculateSourcePosition(37));
        //}

        //[TestMethod()]
        //public void Mapping03() {
        //    Assert.AreEqual(46, stream.CalculateWarpedPosition(35));
        //    //Assert.AreEqual(35, stream.CalculateSourcePosition(46));
        //}

        //[TestMethod()]
        //public void Mapping04() {
        //    Assert.AreEqual(52, stream.CalculateWarpedPosition(55));
        //    //Assert.AreEqual(55, stream.CalculateSourcePosition(52));
        //}

        //[TestMethod()]
        //public void Mapping05() {
        //    Assert.AreEqual(55, stream.CalculateWarpedPosition(60));
        //    //Assert.AreEqual(60, stream.CalculateSourcePosition(55));
        //}

        //[TestMethod()]
        //public void Mapping06() {
        //    // test warp with equal offset of boundary mappings
        //    Assert.AreEqual(55, stream.CalculateWarpedPosition(60));
        //    Assert.AreEqual(57, stream.CalculateWarpedPosition(62));
        //    Assert.AreEqual(60, stream.CalculateWarpedPosition(65));

        //    //Assert.AreEqual(60, stream.CalculateSourcePosition(55));
        //    //Assert.AreEqual(62, stream.CalculateSourcePosition(57));
        //    //Assert.AreEqual(65, stream.CalculateSourcePosition(60));
        //}

        //[TestMethod()]
        //public void Mapping07() {
        //    Assert.AreEqual(64, stream.CalculateWarpedPosition(67));
        //    //Assert.AreEqual(67, stream.CalculateSourcePosition(64));
        //}

        //[TestMethod()]
        //public void Mapping08() {
        //    Assert.AreEqual(70, stream.CalculateWarpedPosition(70));
        //    //Assert.AreEqual(70, stream.CalculateSourcePosition(70));
        //}

        [TestMethod()]
        public void SetPosition01() {
            var audioProperties = new AudioProperties(2, 44100, 32, AudioFormat.IEEE);
            TimeWarpStream s = new TimeWarpStream(
                new NullStream(audioProperties, TimeUtil.TimeSpanToBytes(new TimeSpan(0, 1, 0), audioProperties)));
            TimeSpan length = TimeUtil.BytesToTimeSpan(s.Length, s.Properties);
            s.Mappings.Add(new TimeWarp {
                From = new TimeSpan(length.Ticks / 2),
                To = new TimeSpan(length.Ticks / 4)
            });
            s.Mappings.Add(new TimeWarp {
                From = length,
                To = new TimeSpan(length.Ticks / 4 * 2)
            });

            byte[] buffer = new byte[5000];
            int bytesRead;
            long totalBytesRead;

            Assert.AreEqual(0, s.Position);
            totalBytesRead = 0;
            while ((bytesRead = s.Read(buffer, 0, buffer.Length)) > 0) {
                totalBytesRead += bytesRead;
            }
            Assert.AreEqual(s.Length, totalBytesRead);
            Assert.AreEqual(s.Length, s.Position);
        }

        [TestMethod()]
        public void SetPosition02() {
            var audioProperties = new AudioProperties(2, 44100, 32, AudioFormat.IEEE);
            TimeWarpStream s = new TimeWarpStream(
                new NullStream(audioProperties, TimeUtil.TimeSpanToBytes(new TimeSpan(0, 1, 0), audioProperties)));
            TimeSpan length = TimeUtil.BytesToTimeSpan(s.Length, s.Properties);
            //s.Mappings.Add(new TimeWarp {
            //    From = StreamUtil.AlignToBlockSize(length / 2, s.SampleBlockSize),
            //    To = StreamUtil.AlignToBlockSize(length / 4, s.SampleBlockSize)
            //});
            s.Mappings.Add(new TimeWarp {
                From = length,
                To = new TimeSpan(length.Ticks / 4 * 2)
            });

            byte[] buffer = new byte[5000];
            int bytesRead;
            long totalBytesRead;

            Assert.AreEqual(0, s.Position);
            totalBytesRead = 0;
            s.Position = 11104;
            Assert.AreEqual(11104, s.Position);
            //Assert.AreEqual(0, s.BufferedBytes);
            while ((bytesRead = s.Read(buffer, 0, buffer.Length)) > 0) {
                totalBytesRead += bytesRead;
            }
            Assert.AreEqual(totalBytesRead, s.Position - 11104);
            Assert.AreEqual(s.Length, s.Position);
        }

        [TestMethod()]
        public void SetPosition03() {
            var audioProperties = new AudioProperties(2, 44100, 32, AudioFormat.IEEE);
            TimeWarpStream s = new TimeWarpStream(
                new NullStream(audioProperties, TimeUtil.TimeSpanToBytes(new TimeSpan(0, 1, 0), audioProperties)));
            TimeSpan length = TimeUtil.BytesToTimeSpan(s.Length, s.Properties);
            s.Mappings.Add(new TimeWarp {
                From = new TimeSpan(length.Ticks / 2),
                To = new TimeSpan(length.Ticks / 4)
            });
            s.Mappings.Add(new TimeWarp {
                From = length,
                To = new TimeSpan(length.Ticks / 4 * 2)
            });

            byte[] buffer = new byte[5000];

            Assert.AreEqual(0, s.Position);
            s.Read(buffer, 0, buffer.Length);
            s.Position = 44440;
            while (s.Read(buffer, 0, buffer.Length) > 0) { }
            Assert.AreEqual(s.Length, s.Position);
        }

        [TestMethod()]
        public void SetPosition04() {
            var audioProperties = new AudioProperties(2, 44100, 32, AudioFormat.IEEE);
            TimeWarpStream s = new TimeWarpStream(
                new NullStream(audioProperties, TimeUtil.TimeSpanToBytes(new TimeSpan(0, 1, 0), audioProperties)));
            TimeSpan length = TimeUtil.BytesToTimeSpan(s.Length, s.Properties);
            s.Mappings.Add(new TimeWarp {
                From = new TimeSpan(length.Ticks / 2),
                To = new TimeSpan(length.Ticks / 4)
            });
            s.Mappings.Add(new TimeWarp {
                From = length,
                To = new TimeSpan(length.Ticks / 4 * 2)
            });

            byte[] buffer = new byte[5000];

            Assert.AreEqual(0, s.Position);
            bool positionSet = false;
            int count = 0;
            while (s.Read(buffer, 0, buffer.Length) > 0) {
                if (++count == 5) {
                    positionSet = true;
                    long posBefore = s.Position;
                    //long sourcePosBefore = s.SourceStream.Position - s.BufferedBytes;
                    s.Position = posBefore;
                    Assert.AreEqual(posBefore, s.Position);
                    //Assert.AreEqual(sourcePosBefore, s.SourceStream.Position);
                }
            }
            Assert.IsTrue(positionSet); // if the position hasn't been set, the whole test case is pointless
            Assert.AreEqual(s.Length, s.Position);
        }
    }
}
