using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aurio;
using System.Diagnostics;

namespace Aurio.UnitTest {
    [TestClass]
    public class VolumeUtilTest {
        [TestMethod]
        public void VolumeConversion() {
            for (double x = 0d; x < 1000d; x += 0.00123d) {
                double result = VolumeUtil.DecibelToLinear(VolumeUtil.LinearToDecibel(x));
                Debug.WriteLine("{0}: L2D={1} -> {2}", x, VolumeUtil.LinearToDecibel(x), result);
                Assert.AreEqual(x, result, 0.000001d);
            }
        }
    }
}
