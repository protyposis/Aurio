using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Aurio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class FFTUtilTest
    {
        [TestMethod]
        public void CalculateNextPowerOf2()
        {
            var testPairs = new List<(int, int)>()
            {
                (1, 1),
                (2, 2),
                (3, 4),
                (4, 4),
                (22756, 32768),
                (65536, 65536)
            };

            foreach ((int input, int expected) in testPairs)
            {
                Assert.AreEqual(expected, FFTUtil.CalculateNextPowerOf2(input));
            }
        }

        [TestMethod]
        public void IsPowerOf2()
        {
            Assert.IsTrue(FFTUtil.IsPowerOf2(2));
            Assert.IsTrue(FFTUtil.IsPowerOf2(4));
            Assert.IsTrue(FFTUtil.IsPowerOf2(256));
            Assert.IsFalse(FFTUtil.IsPowerOf2(255));
            Assert.IsFalse(FFTUtil.IsPowerOf2(257));
        }
    }
}
