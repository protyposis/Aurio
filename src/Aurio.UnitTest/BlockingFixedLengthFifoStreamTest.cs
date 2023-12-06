using System;
using Aurio.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest
{
    [TestClass]
    public class BlockingFixedLengthFifoStreamTest
    {
        [TestMethod]
        public void WriteReadSequenceTestBlocking()
        {
            FixedLengthFifoStreamTest.WriteReadSequenceTest(
                new BlockingFixedLengthFifoStream(
                    new AudioProperties(1, 1, 8, AudioFormat.LPCM),
                    10
                )
            );
        }
    }
}
