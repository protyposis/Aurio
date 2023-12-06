using System;
using Aurio.Resampler;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    /// <summary>
    ///This is a test class for TimeWarpCollectionTest and is intended
    ///to contain all TimeWarpCollectionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TimeWarpCollectionTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
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
        //public void MyTestInitialize() {
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            ResamplerFactory.Factory = new MockResamplerFactory();
        }

        [TestMethod()]
        public void TWC1()
        {
            TimeWarpCollection collection = new TimeWarpCollection();
            collection.Add(
                new TimeWarp() { From = new TimeSpan(0, 0, 5), To = new TimeSpan(0, 0, 0) }
            );
            collection.Add(
                new TimeWarp() { From = new TimeSpan(0, 0, 15), To = new TimeSpan(0, 0, 15) }
            );

            Assert.AreEqual(
                new TimeSpan(0, 0, -5),
                collection.TranslateSourceToWarpedPosition(new TimeSpan(0, 0, 0))
            );
            Assert.AreEqual(
                new TimeSpan(0, 0, 0),
                collection.TranslateSourceToWarpedPosition(new TimeSpan(0, 0, 5))
            );
            Assert.AreEqual(
                new TimeSpan(0, 0, 0, 7, 500),
                collection.TranslateSourceToWarpedPosition(new TimeSpan(0, 0, 10))
            );
            Assert.AreEqual(
                new TimeSpan(0, 0, 15),
                collection.TranslateSourceToWarpedPosition(new TimeSpan(0, 0, 15))
            );
            Assert.AreEqual(
                new TimeSpan(0, 0, 20),
                collection.TranslateSourceToWarpedPosition(new TimeSpan(0, 0, 20))
            );
        }

        [TestMethod()]
        public void TranslationRangeTest()
        {
            TimeWarpCollection collection = new TimeWarpCollection();
            collection.Add(
                new TimeWarp() { From = new TimeSpan(0, 0, -5), To = new TimeSpan(0, 0, -5) }
            );
            collection.Add(
                new TimeWarp() { From = new TimeSpan(0, 0, 10), To = new TimeSpan(0, 0, 10) }
            );
            collection.Add(
                new TimeWarp() { From = new TimeSpan(0, 0, 20), To = new TimeSpan(0, 0, 20) }
            );
            collection.Add(
                new TimeWarp() { From = new TimeSpan(0, 0, 40), To = new TimeSpan(0, 0, 40) }
            );

            for (int x = -10; x <= 50; x++)
            {
                Assert.AreEqual(
                    new TimeSpan(0, 0, x),
                    collection.TranslateSourceToWarpedPosition(new TimeSpan(0, 0, x))
                );
            }
        }
    }
}
