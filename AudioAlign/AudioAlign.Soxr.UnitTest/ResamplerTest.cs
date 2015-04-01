using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AudioAlign.Soxr.UnitTest {
    [TestClass]
    public class ResamplerTest {

        [TestMethod]
        public void CreateInstance() {
            var r = new SoxResampler(44100, 96000, 2);
            Assert.IsNotNull(r);
        }

        [TestMethod]
        public void CreateAndDestroyLotsOfInstancesAndProcess() {
            var r = new SoxResampler(44100, 96000, 2);
            int count = 1000;
            var instances = new SoxResampler[count];

            var dataIn = new byte[80000];
            var dataOut = new byte[80000];
            int readIn = 0, readOut = 0;

            for (int i = 0; i < count; i++) {
                instances[i] = new SoxResampler(5.0, 1.0, 2, QualityRecipe.SOXR_HQ, QualityFlags.SOXR_VR);
                instances[i].Process(dataIn, 0, dataIn.Length, dataOut, 0, dataOut.Length, false, out readIn, out readOut);
            }

            for (int i = 0; i < count; i++) {
                instances[i].Dispose();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SoxrException), "Invalid instantiation parameter didn't raise exception")]
        public void CreateInvalidInstance() {
            /* A negative input rate is invalid and should return in an error that triggers an exception.
             * Other invalid parameters should also trigger an exception, but we do not test them all. This test
             * only assures that the error reading works correctly. */
            new SoxResampler(-44100, 96000, 2);
        }

        [TestMethod]
        public void CreateVariableRateInstance() {
            var r = new SoxResampler(2.0, 1.0, 1, QualityRecipe.SOXR_HQ, QualityFlags.SOXR_VR);
        }

        [TestMethod]
        [ExpectedException(typeof(SoxrException), "Invalid instantiation parameter didn't raise exception")]
        public void CreateInvalidVariableRateInstance() {
            var r = new SoxResampler(2.0, 1.0, 1, QualityRecipe.SOXR_LQ, QualityFlags.SOXR_VR);
        }

        [TestMethod]
        public void VersionReturnsString() {
            var r = new SoxResampler(44100, 96000, 2);
            string v = r.Version;

            StringAssert.StartsWith(v, "libsoxr-", "invalid version string returned");

            Console.WriteLine(v);
        }

        [TestMethod]
        public void EngineReturnsString() {
            var r = new SoxResampler(44100, 96000, 2);
            string e = r.Engine;

            Assert.IsNotNull(e, "no engine string returned");

            Console.WriteLine(e);
        }

        [TestMethod]
        public void ClearInternalState() {
            var r = new SoxResampler(44100, 96000, 2);
            r.Clear();
        }

        [TestMethod]
        public void CheckDelay() {
            var r = new SoxResampler(44100, 96000, 2);

            // When no samples have been fed to the resampler, there can't be an output delay
            Assert.AreEqual(0, r.GetOutputDelay(), "unexpected output delay");
        }

        [TestMethod]
        public void ProcessWithoutResampling() {
            var r = new SoxResampler(1.0d, 1.0d, 1);

            int inSize = 12;
            int outSize = 12;

            var sampleDataIn = new byte[inSize];
            var sampleDataOut = new byte[outSize];
            int inputLengthUsed = 0;
            int outputLengthGenerated = 0;

            int remainingIn = inSize;
            int totalIn = 0, totalOut = 0;

            do {
                r.Process(sampleDataIn, 0, remainingIn, sampleDataOut, 0, outSize, 
                    remainingIn == 0, out inputLengthUsed, out outputLengthGenerated);
                totalIn += inputLengthUsed;
                totalOut += outputLengthGenerated;
                remainingIn -= inputLengthUsed;
            }
            while (inputLengthUsed > 0 || outputLengthGenerated > 0);

            Assert.AreEqual(inSize, totalIn, "not all data has been read");
            Assert.AreEqual(outSize, totalOut, "not all data has been put out");
        }

        [TestMethod]
        public void ProcessHugeBlock() {
            var r = new SoxResampler(1.0d, 1.0d, 1);

            int inSize = 15360;
            int outSize = 15360;

            var sampleDataIn = new byte[inSize];
            var sampleDataOut = new byte[outSize];
            int inputLengthUsed = 0;
            int outputLengthGenerated = 0;

            int remainingIn = inSize;
            int totalIn = 0, totalOut = 0;

            do {
                r.Process(sampleDataIn, 0, remainingIn, sampleDataOut, 0, outSize,
                    remainingIn == 0, out inputLengthUsed, out outputLengthGenerated);
                totalIn += inputLengthUsed;
                totalOut += outputLengthGenerated;
                remainingIn -= inputLengthUsed;
            }
            while (inputLengthUsed > 0 || outputLengthGenerated > 0);

            Assert.AreEqual(inSize, totalIn, "not all data has been read");
            Assert.AreEqual(outSize, totalOut, "not all data has been put out");
        }

        [TestMethod]
        public void ProcessRateDouble() {
            var r = new SoxResampler(48000, 96000, 1);

            int inSize = 12;
            int outSize = 12;

            var sampleDataIn = new byte[inSize];
            var sampleDataOut = new byte[outSize];
            int inputLengthUsed = 0;
            int outputLengthGenerated = 0;

            int remainingIn = inSize;
            int totalIn = 0, totalOut = 0;

            do {
                r.Process(sampleDataIn, 0, remainingIn, sampleDataOut, 0, outSize,
                    remainingIn == 0, out inputLengthUsed, out outputLengthGenerated);
                totalIn += inputLengthUsed;
                totalOut += outputLengthGenerated;
                remainingIn -= inputLengthUsed;
            }
            while (inputLengthUsed > 0 || outputLengthGenerated > 0);

            Assert.AreEqual(inSize, totalIn, "not all data has been read");
            Assert.AreEqual(inSize * 2, totalOut, "not all data has been put out");
        }

        [TestMethod]
        public void ProcessRateHalf() {
            var r = new SoxResampler(1.0d, 0.5d, 1);

            int inSize = 4 * 10;
            int outSize = 12;

            var sampleDataIn = new byte[inSize];
            var sampleDataOut = new byte[outSize];
            int inputLengthUsed = 0;
            int outputLengthGenerated = 0;

            int remainingIn = inSize;
            int totalIn = 0, totalOut = 0;

            do {
                r.Process(sampleDataIn, 0, remainingIn, sampleDataOut, 0, outSize,
                    remainingIn == 0, out inputLengthUsed, out outputLengthGenerated);
                totalIn += inputLengthUsed;
                totalOut += outputLengthGenerated;
                remainingIn -= inputLengthUsed;
            }
            while (inputLengthUsed > 0 || outputLengthGenerated > 0);

            Assert.AreEqual(inSize, totalIn, "not all data has been read");
            Assert.AreEqual(inSize / 2, totalOut, "not all data has been put out");
        }

        [TestMethod]
        [ExpectedException(typeof(SoxrException), "Illegal call didn't raise exception")]
        public void IllegalRateChange() {
            var r = new SoxResampler(1.0, 1.0, 1);

            // This call is illegal because a fixed-rate resampler was instantiated
            r.SetIoRatio(2.0, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(SoxrException), "Invalid rate change didn't raise exception")]
        public void InvalidRateChangeAboveMax() {
            var r = new SoxResampler(2.0, 1.0, 1, QualityRecipe.SOXR_HQ, QualityFlags.SOXR_VR);

            // This ratio is invalid because it is higher than the max ratio specified in the constructor
            r.SetIoRatio(3.0, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(SoxrException), "Invalid rate change didn't raise exception")]
        public void InvalidRateChangeNegative() {
            var r = new SoxResampler(2.0, 1.0, 1, QualityRecipe.SOXR_HQ, QualityFlags.SOXR_VR);

            // A negative ratio is impossible
            r.SetIoRatio(-1.0, 0);
        }

        [TestMethod]
        public void RateChange() {
            var r = new SoxResampler(2.0, 1.0, 1, QualityRecipe.SOXR_HQ, QualityFlags.SOXR_VR);

            r.SetIoRatio(1.5, 0);
        }

        [TestMethod]
        public void DisposeTest() {
            var r = new SoxResampler(1.0, 1.0, 1);
            
            r.Dispose();
            r.Dispose(); // call a second time to check for repeated calls working
        }
    }
}
