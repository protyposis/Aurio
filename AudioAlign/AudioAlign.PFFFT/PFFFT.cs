using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioAlign.PFFFT {
    public unsafe class PFFFT : IDisposable {

        private int size;
        private IntPtr setup;
        private IntPtr alignedBuffer1;
        private IntPtr alignedBuffer2;

        public PFFFT(int size, Transform transform) {
            if ((size * 4) % 16 != 0) {
                // For more info see pffft.h
                throw new Exception("invalid size, must be aligned to a 16-byte boundary");
            }

            this.size = size;
            setup = InteropWrapper.pffft_new_setup(size, transform);

            if (size >= 16384) {
                Console.WriteLine("WARNING: size too large, might result in low performance");
                // TODO if this ever gets a problem, implement a "work" area, see pffft.h @ pffft_transform
            }

            uint bufferByteSize = (uint)size * 4;
            alignedBuffer1 = InteropWrapper.pffft_aligned_malloc(new UIntPtr(bufferByteSize));
            alignedBuffer2 = InteropWrapper.pffft_aligned_malloc(new UIntPtr(bufferByteSize));
        }

        private void Transform(float* input, float* output, Direction direction) {
            // The non-ordered transform pffft_transform may be faster, 
            // but all AudioAlign algorithms expect the canonical ordered form
            InteropWrapper.pffft_transform_ordered(setup, input, output, null, direction);
        }

        private void CheckSize(float[] array) {
            if (array.Length != size) {
                throw new Exception("invalid size (expected " + size + ", given " + array.Length + ")");
            }
        }

        public void Forward(float[] inPlaceBuffer) {
            CheckSize(inPlaceBuffer);

            Marshal.Copy(inPlaceBuffer, 0, alignedBuffer1, inPlaceBuffer.Length);
            Transform((float*)alignedBuffer1, (float*)alignedBuffer1, Direction.Forward);
            Marshal.Copy(alignedBuffer1, inPlaceBuffer, 0, inPlaceBuffer.Length);
        }

        public void Forward(float[] input, float[] output) {
            CheckSize(input);
            CheckSize(output);

            Marshal.Copy(input, 0, alignedBuffer1, input.Length);
            Transform((float*)alignedBuffer1, (float*)alignedBuffer2, Direction.Forward);
            Marshal.Copy(alignedBuffer2, output, 0, output.Length);
        }

        public static int SimdSize {
            get { return InteropWrapper.pffft_simd_size(); }
        }

        public void Dispose() {
            if (setup != IntPtr.Zero) {
                InteropWrapper.pffft_destroy_setup(setup);
                setup = IntPtr.Zero;
                InteropWrapper.pffft_aligned_free(alignedBuffer1);
                InteropWrapper.pffft_aligned_free(alignedBuffer2);
            }
        }

        ~PFFFT() {
            Dispose();
        }
    }
}
