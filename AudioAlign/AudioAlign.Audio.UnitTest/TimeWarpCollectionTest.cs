using AudioAlign.Audio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace AudioAlign.Audio.UnitTest
{
    
    
    /// <summary>
    ///This is a test class for TimeWarpCollectionTest and is intended
    ///to contain all TimeWarpCollectionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TimeWarpCollectionTest {


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

        [TestMethod()]
        public void TWC1() {
            TimeWarpCollection collection = new TimeWarpCollection();
            collection.Add(new TimeWarp() { From = 5, To = 0 });
            collection.Add(new TimeWarp() { From = 15, To = 15 });

            Assert.AreEqual(-5, collection.TranslateSourceToWarpedPosition(0));
            Assert.AreEqual(0, collection.TranslateSourceToWarpedPosition(5));
            Assert.AreEqual(7, collection.TranslateSourceToWarpedPosition(10));
            Assert.AreEqual(15, collection.TranslateSourceToWarpedPosition(15));
            Assert.AreEqual(20, collection.TranslateSourceToWarpedPosition(20));
        }

        [TestMethod()]
        public void TranslationRangeTest() {
            TimeWarpCollection collection = new TimeWarpCollection();
            collection.Add(new TimeWarp() { From = -5, To = -5 });
            collection.Add(new TimeWarp() { From = 10, To = 10 });
            collection.Add(new TimeWarp() { From = 20, To = 20 });
            collection.Add(new TimeWarp() { From = 40, To = 40 });

            for (long x = -10; x <= 50; x++) {
                Assert.AreEqual(x, collection.TranslateSourceToWarpedPosition(x));
            }
        }
    }
}
