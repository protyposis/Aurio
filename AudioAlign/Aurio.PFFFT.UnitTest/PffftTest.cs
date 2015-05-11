using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AudioAlign.PFFFT.UnitTest {
    [TestClass]
    public class PffftTest {

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {
            // Init the class before the tests run, so DLLs get loaded and test runtimes 
            // of the first test are not wrong due to initialization
            new PFFFT(64, Transform.Real);
        }

        [TestMethod]
        public void TestSimdSize() {
            int size = PFFFT.SimdSize;
            Console.WriteLine("simd size: " + size);
            Assert.IsTrue(size > 0, "invalid size");
        }

        [TestMethod]
        public void CreateInstanceReal() {
            var pffft = new PFFFT(4096, Transform.Real);
        }

        [TestMethod]
        public void CreateInstanceComplex() {
            var pffft = new PFFFT(4096, Transform.Complex);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreateInstanceWithWrongAlignment() {
            var pffft = new PFFFT(4095, Transform.Real);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TransformTooSmall() {
            int size = 4096;
            var pffft = new PFFFT(size, Transform.Real);
            var data = new float[size - 1];

            pffft.Forward(data);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TransformTooBig() {
            int size = 4096;
            var pffft = new PFFFT(size, Transform.Real);
            var data = new float[size + 1];

            pffft.Forward(data);
        }

        [TestMethod]
        public void TransformForwardInPlace() {
            int size = 4096;
            var pffft = new PFFFT(size, Transform.Real);
            var data = new float[size];

            pffft.Forward(data);
        }

        [TestMethod]
        public void TransformForward() {
            int size = 4096;
            var pffft = new PFFFT(size, Transform.Real);
            var dataIn = new float[size];
            var dataOut = new float[size];

            pffft.Forward(dataIn, dataOut);
        }
    }
}
