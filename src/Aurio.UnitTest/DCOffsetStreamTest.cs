using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class DCOffsetStreamTest
    {
        [TestMethod]
        public void DCOffsetApplied()
        {
            var offset = 0.2f;
            var input = new float[] { -1.3f, -0.5f, 0, 0.1f, 1f };
            var s = new DCOffsetStream(new MemorySourceStream(input, 100, 1), offset);

            var output = StreamUtil.ReadFloats(s, input.Length);

            CollectionAssert.AreEqual(input.Select(x => x + offset).ToArray(), output);
        }
    }
}
