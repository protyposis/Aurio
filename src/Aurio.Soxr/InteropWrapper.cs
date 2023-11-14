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
using SoxrError = System.IntPtr;
using SoxrInstance = System.IntPtr;
using StringPtr = System.IntPtr;

namespace Aurio.Soxr
{
    internal unsafe class InteropWrapper
    {
        /*
         * The following structs must be structs and cannot be nullable classes, because the corresponding
         * config functions return structs directly, not pointers to structs (in which case classes could be
         * used). The disadvantage with structs is that they cannot be nulled, which means that the soxr_create
         * function must always be supplied with valid structs, although soxr would configure itself to its
         * default values when passing nulls instead. Passing nulls would be possible when creating additional
         * soxr_create mappings with IntPtrs instead of ref structs as parameters, but to cover all cases it
         * would mean to add 7 additional variants (all possible kinds of IntPtr/struct combinations).
         */

        [StructLayout(LayoutKind.Sequential)]
        public struct SoxrIoSpec
        {
            public Datatype itype;
            public Datatype otype;
            public double scale;
            public void* e;
            public IoFlags flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SoxrQualitySpec
        {
            public double precision;
            public double phase_response;
            public double passband_end;
            public double stopband_begin;
            public void* e;
            public QualityFlags flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SoxrRuntimeSpec
        {
            public uint log2_min_dft_size;
            public uint log2_large_dft_size;
            public uint coef_size_kbytes;
            public uint num_threads;
            public void* e;
            public RuntimeFlags flags;
        }

        /*
         * The length of the unsigned integer size_t is platform dependent (32/64 bit). As C# data types
         * uint and ulong are fixed length, UIntPtrs must be used for marshalling size_t because they are
         * also platform dependent.
         */

        public delegate StringPtr d_soxr_version();
        public delegate SoxrInstance d_soxr_create(
            double input_rate,
            double output_rate,
            uint num_channels,
            out SoxrError error,
            ref SoxrIoSpec io_spec,
            ref SoxrQualitySpec quality_spec,
            ref SoxrRuntimeSpec runtime_spec
        );
        public delegate SoxrError d_soxr_process(
            SoxrInstance resampler,
            byte* input,
            UIntPtr ilen,
            out UIntPtr idone,
            byte* output,
            UIntPtr olen,
            out UIntPtr odone
        );
        public delegate SoxrError d_soxr_error(SoxrInstance resampler);
        public delegate double d_soxr_delay(SoxrInstance resampler);
        public delegate StringPtr d_soxr_engine(SoxrInstance resampler);
        public delegate SoxrError d_soxr_clear(SoxrInstance resampler);
        public delegate void d_soxr_delete(SoxrInstance resampler);
        public delegate SoxrError d_soxr_set_io_ratio(
            SoxrInstance resampler,
            double io_ratio,
            UIntPtr slew_len
        );
        public delegate SoxrQualitySpec d_soxr_quality_spec(
            QualityRecipe recipe,
            QualityFlags flags
        );
        public delegate SoxrRuntimeSpec d_soxr_runtime_spec(uint num_threads);
        public delegate SoxrIoSpec d_soxr_io_spec(Datatype itype, Datatype otype);

        public static d_soxr_version soxr_version;
        public static d_soxr_create soxr_create;
        public static d_soxr_process soxr_process;
        public static d_soxr_error soxr_error;
        public static d_soxr_delay soxr_delay;
        public static d_soxr_engine soxr_engine;
        public static d_soxr_clear soxr_clear;
        public static d_soxr_delete soxr_delete;
        public static d_soxr_set_io_ratio soxr_set_io_ratio;
        public static d_soxr_quality_spec soxr_quality_spec;
        public static d_soxr_runtime_spec soxr_runtime_spec;
        public static d_soxr_io_spec soxr_io_spec;

        static InteropWrapper()
        {
            if (Environment.Is64BitProcess)
            {
                soxr_version = Interop64.soxr_version;
                soxr_create = Interop64.soxr_create;
                soxr_process = Interop64.soxr_process;
                soxr_error = Interop64.soxr_error;
                soxr_delay = Interop64.soxr_delay;
                soxr_engine = Interop64.soxr_engine;
                soxr_clear = Interop64.soxr_clear;
                soxr_delete = Interop64.soxr_delete;
                soxr_set_io_ratio = Interop64.soxr_set_io_ratio;
                soxr_quality_spec = Interop64.soxr_quality_spec;
                soxr_runtime_spec = Interop64.soxr_runtime_spec;
                soxr_io_spec = Interop64.soxr_io_spec;
            }
            else
            {
                soxr_version = Interop32.soxr_version;
                soxr_create = Interop32.soxr_create;
                soxr_process = Interop32.soxr_process;
                soxr_error = Interop32.soxr_error;
                soxr_delay = Interop32.soxr_delay;
                soxr_engine = Interop32.soxr_engine;
                soxr_clear = Interop32.soxr_clear;
                soxr_delete = Interop32.soxr_delete;
                soxr_set_io_ratio = Interop32.soxr_set_io_ratio;
                soxr_quality_spec = Interop32.soxr_quality_spec;
                soxr_runtime_spec = Interop32.soxr_runtime_spec;
                soxr_io_spec = Interop32.soxr_io_spec;
            }
        }
    }
}
