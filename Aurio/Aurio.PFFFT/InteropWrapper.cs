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
using System.Text;

namespace Aurio.PFFFT
{
    internal unsafe class InteropWrapper
    {

        public delegate IntPtr d_pffft_new_setup(int size, Transform transform);
        public delegate void d_pffft_destroy_setup(IntPtr setup);
        public delegate void d_pffft_transform(IntPtr setup, float* input, float* output, float* work, Direction direction);
        public delegate void d_pffft_transform_ordered(IntPtr setup, float* input, float* output, float* work, Direction direction);
        public delegate void d_pffft_zreorder(IntPtr setup, float* input, float* output, Direction direction);
        public delegate void d_pffft_zconvolve_accumulate(IntPtr setup, float* dft_a, float* dft_b, float* dft_ab, float scaling);
        public delegate IntPtr d_pffft_aligned_malloc(UIntPtr nb_bytes);
        public delegate void d_pffft_aligned_free(IntPtr p);
        public delegate int d_pffft_simd_size();

        public static d_pffft_new_setup pffft_new_setup;
        public static d_pffft_destroy_setup pffft_destroy_setup;
        public static d_pffft_transform pffft_transform;
        public static d_pffft_transform_ordered pffft_transform_ordered;
        public static d_pffft_zreorder pffft_zreorder;
        public static d_pffft_zconvolve_accumulate pffft_zconvolve_accumulate;
        public static d_pffft_aligned_malloc pffft_aligned_malloc;
        public static d_pffft_aligned_free pffft_aligned_free;
        public static d_pffft_simd_size pffft_simd_size;

        static InteropWrapper()
        {
            if (Environment.Is64BitProcess)
            {
                pffft_new_setup = Interop64.pffft_new_setup;
                pffft_destroy_setup = Interop64.pffft_destroy_setup;
                pffft_transform = Interop64.pffft_transform;
                pffft_transform_ordered = Interop64.pffft_transform_ordered;
                pffft_zreorder = Interop64.pffft_zreorder;
                pffft_zconvolve_accumulate = Interop64.pffft_zconvolve_accumulate;
                pffft_aligned_malloc = Interop64.pffft_aligned_malloc;
                pffft_aligned_free = Interop64.pffft_aligned_free;
                pffft_simd_size = Interop64.pffft_simd_size;
            }
            else
            {
                pffft_new_setup = Interop32.pffft_new_setup;
                pffft_destroy_setup = Interop32.pffft_destroy_setup;
                pffft_transform = Interop32.pffft_transform;
                pffft_transform_ordered = Interop32.pffft_transform_ordered;
                pffft_zreorder = Interop32.pffft_zreorder;
                pffft_zconvolve_accumulate = Interop32.pffft_zconvolve_accumulate;
                pffft_aligned_malloc = Interop32.pffft_aligned_malloc;
                pffft_aligned_free = Interop32.pffft_aligned_free;
                pffft_simd_size = Interop32.pffft_simd_size;
            }
        }
    }
}
