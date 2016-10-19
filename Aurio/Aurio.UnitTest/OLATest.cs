using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aurio.Features;
using Aurio.Streams;

namespace Aurio.UnitTest {
    [TestClass]
    public class OLATest {

        [TestMethod]
        public void CreateMonoIEEE() {
            var properties = new AudioProperties(1, 44100, 16, AudioFormat.IEEE);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 500);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateMonoLPCM() {
            var properties = new AudioProperties(1, 44100, 16, AudioFormat.LPCM);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 500);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateStereoIEEE() {
            var properties = new AudioProperties(2, 44100, 16, AudioFormat.IEEE);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 500);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CreateOverlapTooLarge() {
            var properties = new AudioProperties(1, 44100, 16, AudioFormat.IEEE);
            var stream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);
            new OLA(stream, 1000, 1001);
        }

        [TestMethod]
        public void MonoOverlapAddRectangle() {
            var properties = new AudioProperties(1, 44100, 32, AudioFormat.IEEE);
            var sourceStream = new SineGeneratorStream(44100, 440, TimeSpan.FromSeconds(1));
            var targetStream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);

            // Rectangular window (samples unchanged) with no overlap should just reconstruct the original stream
            int windowSize = 100;
            int hopSize = 100;
            var window = WindowType.Rectangle;

            var sw = new StreamWindower(sourceStream, windowSize, hopSize, window);
            var ola = new OLA(targetStream, windowSize, hopSize);

            var frameBuffer = new float[windowSize];
            while (sw.HasNext()) {
                sw.ReadFrame(frameBuffer);
                ola.WriteFrame(frameBuffer);
            }
            ola.Flush();

            Assert.AreEqual(sourceStream.Length, targetStream.Length);

            // Validate ola target stream content
            sourceStream.Position = targetStream.Position = 0;
            long similarFloats = StreamUtil.CompareFloats(sourceStream, targetStream);
            Assert.AreEqual(sourceStream.Length / sourceStream.SampleBlockSize, similarFloats);
        }

        [TestMethod]
        public void MonoOverlapAddHann() {
            var properties = new AudioProperties(1, 44100, 32, AudioFormat.IEEE);
            IAudioStream sourceStream = new SineGeneratorStream(44100, 440, TimeSpan.FromSeconds(1));
            IAudioWriterStream targetStream = new MemoryWriterStream(new System.IO.MemoryStream(), properties);

            // 50% overlap with a Hann window is an optimal combination, Hann window is optimally uneven with a 1 as middle point
            int windowSize = 21;
            int hopSize = 11;
            var window = WindowType.Hann;

            // Adjust source length to window/hop size so no samples remain at the end 
            // (a potential last incomplete frame is not returned by the stream windower)
            // With remaining samples, the source and target length cannot be compared
            sourceStream = new CropStream(sourceStream, 0, 
                sourceStream.Length 
                - (sourceStream.Length - windowSize * sourceStream.SampleBlockSize) 
                % (hopSize * sourceStream.SampleBlockSize));

            var sw = new StreamWindower(sourceStream, windowSize, hopSize, window);
            var ola = new OLA(targetStream, windowSize, hopSize);

            var frameBuffer = new float[windowSize];
            while(sw.HasNext()) {
                sw.ReadFrame(frameBuffer);
                ola.WriteFrame(frameBuffer);
            }
            ola.Flush();

            Assert.AreEqual(sourceStream.Length, targetStream.Length);

            // Validate ola target stream content
            // Crop the streams to ignore windowed start and end when no overlap-add is performed and content definitely differs

            var sourceStreamCropped = new CropStream(sourceStream, 
                hopSize * sourceStream.SampleBlockSize*2, 
                sourceStream.Length - hopSize * sourceStream.SampleBlockSize);

            var targetStreamCropped = new CropStream(targetStream,
                hopSize * sourceStream.SampleBlockSize*2,
                sourceStream.Length - hopSize * sourceStream.SampleBlockSize);

            sourceStreamCropped.Position = targetStreamCropped.Position = 0;

            long similarFloats = StreamUtil.CompareFloats(sourceStreamCropped, targetStreamCropped);

            Assert.AreEqual(sourceStreamCropped.Length / sourceStreamCropped.SampleBlockSize, similarFloats);
        }
    }
}
