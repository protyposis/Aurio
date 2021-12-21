using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aurio;

namespace Aurio.UnitTest
{
    [TestClass]
    public class AudioBufferTest
    {
        [TestMethod]
        public void AudioBufferConstructor1()
        {
            AudioBuffer b = new AudioBuffer(64);

            Assert.AreEqual(64, b.ByteSize);
            Assert.AreEqual(16, b.FloatSize);

            b.ByteData[0] = 100;
            b.ByteData[1] = 50;

            AudioBuffer b2 = new AudioBuffer(64);
            b2.FloatData[0] = b.FloatData[0];

            for (int x = 0; x < b.ByteSize; x++)
            {
                Assert.AreEqual(b.ByteData[x], b2.ByteData[x]);
            }
        }

        [TestMethod]
        public void AudioBufferConstructor2()
        {
            float f1 = 155.55f;
            float f2 = 765.432f;

            byte[] array = new byte[64];
            // inject float values into the byte array
            unsafe
            {
                fixed (byte* arrayB = &array[0])
                {
                    float* arrayF = (float*)arrayB;
                    arrayF[0] = f1;
                    arrayF[5] = f2;
                }
            }

            AudioBuffer b = new AudioBuffer(array);

            // check if the float can be read from the conversion buffer
            Assert.AreEqual(f1, b.FloatData[0]);
            Assert.AreEqual(f2, b.FloatData[5]);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException), "cannot read all float bytes, as expected")]
        public void AudioBufferConstructor3()
        {
            float[] array = new float[1];
            AudioBuffer b = new AudioBuffer(array);

            // try to read all 4 bytes from the float
            /* at index 1 it will fail since the CLR thinks that the byte array has a length of 1 (happens 
             * because it has been initialized with a float array of length one and both are mapped to the 
             * same memory address)
             */
            for (int x = 0; x < 4; x++)
            {
                byte floatByte = b.ByteData[x];
            }
        }
    }
}
