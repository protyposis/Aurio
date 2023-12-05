using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class WindowUtilTest
    {
        [TestMethod]
        public void Triangle()
        {
            var window = WindowUtil.GetArray(WindowType.Triangle, 4);
            CollectionAssert.AreEqual(window, new float[] { 0.25f, 0.75f, 0.75f, 0.25f });
        }

        [TestMethod]
        public void Hann()
        {
            var window = WindowUtil.GetArray(WindowType.Hann, 5);
            CollectionAssert.AreEqual(window, new float[] { 0, 0.5f, 1, 0.5f, 0 });
        }

        [TestMethod]
        public void HannNormalized()
        {
            var window = WindowUtil.GetArray(new WindowConfig(WindowType.Hann, 5, 2));
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
            var window = WindowUtil.GetArray(new WindowConfig(WindowType.HannPeriodic, 6, 2));
            CollectionAssert.AreEqual(window, new float[] { 0, 0.5f, 1.5f, 2, 1.5f, 0.5f });
        }

        [TestMethod]
        public void HammingPeriodic()
        {
            var window = WindowUtil.GetArray(WindowType.HammingPeriodic, 10);
            var expected = new float[]
            {
                0.08f,
                0.167852178f,
                0.397852182f,
                0.6821478f,
                0.9121478f,
                1,
                0.9121478f,
                0.6821478f,
                0.397852182f,
                0.167852178f
            };

            CollectionAssert.AreEqual(expected, window);
        }

        [TestMethod]
        public void Sine()
        {
            var window = WindowUtil.GetArray(WindowType.Sine, 10);
            var expected = new float[]
            {
                0,
                0.342020154f,
                0.642787635f,
                0.8660254f,
                0.9848077f,
                0.9848077f,
                0.8660254f,
                0.642787635f,
                0.342020154f,
                1.22464685E-16f
            };

            CollectionAssert.AreEqual(expected, window);
        }

        [TestMethod]
        public void SinePeriodic()
        {
            var window = WindowUtil.GetArray(WindowType.SinePeriodic, 10);
            var expected = new float[]
            {
                0,
                0.309017f,
                0.587785244f,
                0.809017f,
                0.95105654f,
                1,
                0.95105654f,
                0.809017f,
                0.587785244f,
                0.309017f
            };

            CollectionAssert.AreEqual(expected, window);
        }

        [TestMethod]
        public void FunctionEqualsArray()
        {
            var window = WindowUtil.GetArray(WindowType.Hann, 10);
            var function = WindowUtil.GetFunction(WindowType.Hann, 10);

            var windowFromFunction = Enumerable.Repeat(1f, function.Size).ToArray();
            function.Apply(windowFromFunction);

            CollectionAssert.AreEqual(window, windowFromFunction);
        }

        [TestMethod]
        public void Apply()
        {
            var input = new float[] { 0.5f, 1, 0.5f };
            var window = new float[] { 0.5f, 2, 0.1f };
            var expected = new float[] { 0.25f, 2, 0.05f };

            WindowUtil.Apply(input, 0, window);

            CollectionAssert.AreEqual(expected, input);
        }

        [TestMethod]
        public void NormalizationFactor()
        {
            var window = WindowUtil.GetArray(new WindowConfig(WindowType.Hann, 10, 0.5f));
            var expected = WindowUtil.GetArray(WindowType.Hann, 10).Select(x => x * 0.5f).ToArray();

            CollectionAssert.AreEqual(expected, window);
        }

        [TestMethod]
        public void ConfigIsEquivalentToParameters()
        {
            var window1 = WindowUtil.GetArray(WindowType.Blackman, 20);
            var window2 = WindowUtil.GetArray(new WindowConfig(WindowType.Blackman, 20));

            CollectionAssert.AreEqual(window1, window2);
        }
    }
}
