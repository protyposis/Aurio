using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Aurio.Audio {
    /// <summary>
    /// NOTE This class only works correctly in the byte->float direction. In the other direction there are problems with array length and access.
    /// Inspiration taken from: http://mark-dot-net.blogspot.com/2008/06/wavebuffer-casting-byte-arrays-to-float.html
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public class AudioBuffer {
        [FieldOffset(0)]
        private int numberOfBytes;
        [FieldOffset(4)]
        private byte[] byteBuffer;
        [FieldOffset(4)]
        private float[] floatBuffer;

        public AudioBuffer(int byteCapacity) {
            byteBuffer = new byte[byteCapacity];
            numberOfBytes = byteCapacity;
        }

        public AudioBuffer(byte[] buffer) {
            byteBuffer = buffer;
            numberOfBytes = buffer.Length;
        }

        public AudioBuffer(float[] buffer) {
            floatBuffer = buffer;
            numberOfBytes = buffer.Length * 4;
        }

        public byte[] ByteData {
            get { return byteBuffer; }
        }

        public int ByteSize {
            get { return numberOfBytes; }
        }

        public float[] FloatData {
            get { return floatBuffer; }
        }

        public int FloatSize {
            get { return numberOfBytes / 4; }
        }
    }
}
