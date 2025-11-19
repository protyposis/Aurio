using System.Threading;
using System.Threading.Tasks;
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

        [TestMethod]
        [Timeout(5000)]
        public void ReadBlocksUntilDataArrives()
        {
            var props = new AudioProperties(1, 8000, 16, AudioFormat.LPCM);
            var stream = new BlockingFixedLengthFifoStream(props, 1024);
            var buf = new byte[128];

            var readTask = Task.Run(() => stream.Read(buf, 0, buf.Length));

            // ensure the read has time to enter waiting state
            Task.Delay(50).Wait();

            stream.Write(new byte[buf.Length], 0, buf.Length);

            var completed = Task.WaitAny(new[] { readTask }, 2000);
            Assert.AreNotEqual(-1, completed, "Read did not complete after write");

            Assert.AreEqual(
                buf.Length,
                readTask.Result,
                "Read should return requested bytes after data arrives"
            );
        }

        [TestMethod]
        [Timeout(5000)]
        public void ReadUnblocksOnEndSignalWithoutData()
        {
            var props = new AudioProperties(1, 8000, 16, AudioFormat.LPCM);
            var stream = new BlockingFixedLengthFifoStream(props, 1024);
            var buf = new byte[128];

            var readTask = Task.Run(() => stream.Read(buf, 0, buf.Length));
            Task.Delay(50).Wait(); // give time to block
            stream.SignalEndOfInput();

            var completed = Task.WaitAny(new[] { readTask }, 2000);
            Assert.AreNotEqual(-1, completed, "Read did not complete after end-of-input signal");
            Assert.AreEqual(
                0,
                readTask.Result,
                "Read should return 0 at end-of-input with no data"
            );
        }
    }
}
