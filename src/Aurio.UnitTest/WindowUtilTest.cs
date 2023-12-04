using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class WindowUtilTest
    {
        [TestMethod]
        public void Hann()
        {
            var window = WindowUtil.GetArray(WindowType.Hann, 5);
            CollectionAssert.AreEqual(window, new float[] { 0, 0.5f, 1, 0.5f, 0 });
        }

        [TestMethod]
        public void HannNormalized()
        {
            var window = WindowUtil.GetArray(WindowType.Hann, 5, 2);
            CollectionAssert.AreEqual(window, new float[] { 0, 1, 2, 1, 0 });
        }

        [TestMethod]
        public void HannPeriodic()
        {
            var window = WindowUtil.GetArray(WindowType.HannPeriodic, 6);
            CollectionAssert.AreEqual(window, new float[] { 0, 0.25f, 0.75f, 1, 0.75f, 0.25f });
        }

        [TestMethod]
        public void HannPeriodicNormalized()
        {
            var window = WindowUtil.GetArray(WindowType.HannPeriodic, 6, 2);
            CollectionAssert.AreEqual(window, new float[] { 0, 0.5f, 1.5f, 2, 1.5f, 0.5f });
        }
    }
}
