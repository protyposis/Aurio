using AudioAlign.Audio.Matching.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AudioAlign.Audio.UnitTest
{
    
    
    /// <summary>
    ///This is a test class for UndirectedGraphTest and is intended
    ///to contain all UndirectedGraphTest Unit Tests
    ///</summary>
    [TestClass()]
    public class UndirectedGraphTest {


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
        public void AddEdge() {
            UndirectedGraph<object, int> g = new UndirectedGraph<object, int>();
            g.Add(new Edge<object, int>(new Object(), new Object(), 0));
            Assert.AreEqual(1, g.Edges.Count);
            Assert.AreEqual(2, g.Vertices.Count);
        }

        [TestMethod()]
        public void AddEdges2() {
            UndirectedGraph<object, int> g = new UndirectedGraph<object, int>();

            object o1 = new Object();
            object o2 = new Object();
            object o3 = new Object();

            g.Add(new Edge<object, int>(o1, o2, 0));
            g.Add(new Edge<object, int>(o3, o2, 0));
            Assert.AreEqual(2, g.Edges.Count);
            Assert.AreEqual(3, g.Vertices.Count);
        }

        [TestMethod()]
        public void GetEdges() {
            UndirectedGraph<object, int> g = new UndirectedGraph<object, int>();

            object o1 = new Object();
            object o2 = new Object();
            object o3 = new Object();
            g.Add(new Edge<object, int>(o1, o2, 0));
            g.Add(new Edge<object, int>(o3, o2, 0));

            Assert.AreEqual(1, g.GetEdges(o1).Count);
            Assert.AreEqual(2, g.GetEdges(o2).Count);
        }

        [TestMethod()]
        public void IsConnected() {
            UndirectedGraph<object, int> g = new UndirectedGraph<object, int>();

            object o1 = new Object();
            object o2 = new Object();
            object o3 = new Object();
            g.Add(new Edge<object, int>(o1, o2, 0));
            g.Add(new Edge<object, int>(o3, o2, 0));

            Assert.IsTrue(g.IsConnectedBetween(o1, o2));
            Assert.IsTrue(g.IsConnectedBetween(o1, o3));
            Assert.IsTrue(g.IsConnectedBetween(o2, o3));
            Assert.IsFalse(g.IsDisconnected);
        }

        [TestMethod()]
        public void IsDisconnected() {
            UndirectedGraph<object, int> g = new UndirectedGraph<object, int>();

            // component 1
            object c1o1 = new Object();
            object c1o2 = new Object();
            object c1o3 = new Object();
            g.Add(new Edge<object, int>(c1o1, c1o2, 0));
            g.Add(new Edge<object, int>(c1o3, c1o2, 0));

            // component 2
            object c2o1 = new Object();
            object c2o2 = new Object();
            object c2o3 = new Object();
            g.Add(new Edge<object, int>(c2o1, c2o2, 0));
            g.Add(new Edge<object, int>(c2o3, c2o2, 0));

            Assert.IsTrue(g.IsDisconnected);
            Assert.IsFalse(g.IsConnectedBetween(c1o1, c2o1));
        }

        [TestMethod()]
        public void ConnectedComponents() {
            UndirectedGraph<object, int> g = new UndirectedGraph<object, int>();

            // component 1
            object c1o1 = new Object();
            object c1o2 = new Object();
            object c1o3 = new Object();
            g.Add(new Edge<object, int>(c1o1, c1o2, 0));
            g.Add(new Edge<object, int>(c1o3, c1o2, 0));

            // component 2
            object c2o1 = new Object();
            object c2o2 = new Object();
            object c2o3 = new Object();
            g.Add(new Edge<object, int>(c2o1, c2o2, 0));
            g.Add(new Edge<object, int>(c2o3, c2o2, 0));

            var components = g.GetConnectedComponents();

            Assert.AreEqual(2, components.Count);

            Assert.AreEqual(3, components[0].Vertices.Count);
            Assert.AreEqual(2, components[0].Edges.Count);
            Assert.IsTrue(components[0].Vertices.Contains(c1o1));
            Assert.IsFalse(components[0].Vertices.Contains(c2o1));

            Assert.AreEqual(3, components[1].Vertices.Count);
            Assert.AreEqual(2, components[1].Edges.Count);
        }
    }
}
