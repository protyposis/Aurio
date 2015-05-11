using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Aurio.PFFFT {
    internal unsafe class Interop64 {

        private const string PFFFTLIB = "pffft.x64.dll";
        private const CallingConvention CC = CallingConvention.Cdecl;

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern IntPtr pffft_new_setup(int size, Transform transform);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern void pffft_destroy_setup(IntPtr setup);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern void pffft_transform(IntPtr setup, float* input, float* output, float* work, Direction direction);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern void pffft_transform_ordered(IntPtr setup, float* input, float* output, float* work, Direction direction);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern void pffft_zreorder(IntPtr setup, float* input, float* output, Direction direction);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern void pffft_zconvolve_accumulate(IntPtr setup, float* dft_a, float* dft_b, float* dft_ab, float scaling);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern IntPtr pffft_aligned_malloc(UIntPtr nb_bytes);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern void pffft_aligned_free(IntPtr p);

        [DllImport(PFFFTLIB, CallingConvention = CC)]
        public static extern int pffft_simd_size();
    }
}
