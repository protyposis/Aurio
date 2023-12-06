using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class MemorySourceStreamTest
    {
        [TestMethod]
        public void ReadSampleArrayInput()
        {
            var input = new float[] { -1.3f, -0.5f, 0, 0.1f, 1f };
            var s = new MemorySourceStream(input, 100, 1);

            var output = StreamUtil.ReadFloats(s, input.Length);

            CollectionAssert.AreEqual(input, output);
        }
    }
}
