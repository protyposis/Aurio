using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aurio.Streams;
using System.Collections;
using System.Collections.Generic;

namespace Aurio.PFFFT.UnitTest {
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

        [TestMethod]
        public void TransformBackwardInPlace() {
            int size = 4096;
            var pffft = new PFFFT(size, Transform.Real);
            var data = new float[size];

            pffft.Backward(data);
        }

        [TestMethod]
        public void TransformBackward() {
            int size = 4096;
            var pffft = new PFFFT(size, Transform.Real);
            var dataIn = new float[size];
            var dataOut = new float[size];

            pffft.Backward(dataIn, dataOut);
        }

        [TestMethod]
        public void TransformForwardAndBackward() {
            int size = 4096;
            var pffft = new PFFFT(size, Transform.Real);
            var dataIn = new float[size];
            var dataOut = new float[size];
            var dataBack = new float[size];

            var generator = new SineGeneratorStream(4096, 100, TimeSpan.FromSeconds(1));
            generator.Read(dataIn, 0, size);

            pffft.Forward(dataIn, dataOut);
            pffft.Backward(dataOut, dataBack);

            CollectionAssert.AreEqual(dataIn, dataBack, new FFTComparer());
        }

        private class FFTComparer : IComparer, IComparer<float> {

            private const float deltaThreshold = 0.000001f;

            public int Compare(object x, object y) {
                return Compare((float)x, (float)y);
            }

            public int Compare(float x, float y) {
                float delta = x - y;

                if(Math.Abs(delta) < deltaThreshold) {
                    // Both are considered equal if difference is below delta
                    // to account for floating point imprecisions in FFT
                    return 0;
                } else if(delta < 0) {
                    return -1;
                } else {
                    return 1;
                }
            }
        }
    }
}
