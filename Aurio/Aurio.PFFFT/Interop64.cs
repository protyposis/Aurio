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
