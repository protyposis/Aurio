//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.Runtime.InteropServices;

namespace Aurio
{
    /// <summary>
    /// NOTE This class only works correctly in the byte->float direction. In the other direction there are problems with array length and access.
    /// Inspiration taken from: http://mark-dot-net.blogspot.com/2008/06/wavebuffer-casting-byte-arrays-to-float.html
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public class AudioBuffer
    {
        [FieldOffset(0)]
        private int numberOfBytes;

        // Arrays must be DWORD aligned so a field offset of 4 (because int is 4 bytes) does not work here
        // https://stackoverflow.com/a/1190114/370252
        [FieldOffset(8)]
        private byte[] byteBuffer;

        [FieldOffset(8)]
        private float[] floatBuffer;

        public AudioBuffer(int byteCapacity)
        {
            byteBuffer = new byte[byteCapacity];
            numberOfBytes = byteCapacity;
        }

        public AudioBuffer(byte[] buffer)
        {
            byteBuffer = buffer;
            numberOfBytes = buffer.Length;
        }

        public AudioBuffer(float[] buffer)
        {
            floatBuffer = buffer;
            numberOfBytes = buffer.Length * 4;
        }

        public byte[] ByteData
        {
            get { return byteBuffer; }
        }

        public int ByteSize
        {
            get { return numberOfBytes; }
        }

        public float[] FloatData
        {
            get { return floatBuffer; }
        }

        public int FloatSize
        {
            get { return numberOfBytes / 4; }
        }
    }
}
