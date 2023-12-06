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
using System.Runtime.InteropServices;
using SoxrError = System.IntPtr;
using SoxrInstance = System.IntPtr;
using StringPtr = System.IntPtr;

namespace Aurio.Soxr
{
    //[SuppressUnmanagedCodeSecurity]
    internal unsafe class Interop64
    {
        private const string SOXRLIB = "soxr.x64.dll";
        private const CallingConvention CC = CallingConvention.Cdecl;

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern StringPtr soxr_version();

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrInstance soxr_create(
            double input_rate,
            double output_rate,
            uint num_channels,
            out SoxrError error,
            ref InteropWrapper.SoxrIoSpec io_spec,
            ref InteropWrapper.SoxrQualitySpec quality_spec,
            ref InteropWrapper.SoxrRuntimeSpec runtime_spec
        );

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrError soxr_process(
            SoxrInstance resampler,
            byte* input,
            UIntPtr ilen,
            out UIntPtr idone,
            byte* output,
            UIntPtr olen,
            out UIntPtr odone
        );

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrError soxr_error(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern double soxr_delay(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern StringPtr soxr_engine(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrError soxr_clear(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern void soxr_delete(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrError soxr_set_io_ratio(
            SoxrInstance resampler,
            double io_ratio,
            UIntPtr slew_len
        );

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern InteropWrapper.SoxrQualitySpec soxr_quality_spec(
            QualityRecipe recipe,
            QualityFlags flags
        );

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern InteropWrapper.SoxrRuntimeSpec soxr_runtime_spec(uint num_threads);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern InteropWrapper.SoxrIoSpec soxr_io_spec(Datatype itype, Datatype otype);
    }
}
