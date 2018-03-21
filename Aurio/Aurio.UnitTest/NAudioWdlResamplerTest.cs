using Aurio.Resampler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.UnitTest
{
    [TestClass]
    public class NAudioWdlResamplerTest
    {
        [TestMethod]
        public void CreateInstance()
        {
            var r = new NAudioWdlResampler(ResamplingQuality.VariableRate, 1, 1);
            Assert.IsNotNull(r);
        }

        [TestMethod]
        public void CheckDelay()
        {
            var r = new NAudioWdlResampler(ResamplingQuality.VariableRate, 1, 1);

            // When no samples have been fed to the resampler, there can't be an output delay
            Assert.AreEqual(0, r.GetOutputDelay(), "unexpected output delay");
        }

        [TestMethod]
        public void ProcessWithoutResampling()
        {
            var r = new NAudioWdlResampler(ResamplingQuality.High, 1, 1);

            int inSize = 12;
            int outSize = 12;

            var sampleDataIn = new byte[inSize];
            var sampleDataOut = new byte[outSize];
            int inputLengthUsed = 0;
            int outputLengthGenerated = 0;

            int remainingIn = inSize;
            int totalIn = 0, totalOut = 0;

            do
            {
                r.Process(sampleDataIn, 0, remainingIn, sampleDataOut, 0, outSize,
                    remainingIn == 0, out inputLengthUsed, out outputLengthGenerated);
                totalIn += inputLengthUsed;
                totalOut += outputLengthGenerated;
                remainingIn -= inputLengthUsed;
            }
            while (inputLengthUsed > 0 || outputLengthGenerated > 0);

            Assert.AreEqual(inSize, totalIn, "not all data has been read");
            Assert.AreEqual(outSize, totalOut, "not all data has been put out");

            // TODO figure out how to flush the resampler to get all samples out
        }
    }
}
