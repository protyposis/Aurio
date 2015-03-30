using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using StringPtr = System.IntPtr;
using SoxrInstance = System.IntPtr;
using SoxrError = System.IntPtr;
using SoxrIoSpec = System.IntPtr;
using SoxrQualitySpec = System.IntPtr;
using SoxrRuntimeSpec = System.IntPtr;

namespace AudioAlign.Soxr {
    //[SuppressUnmanagedCodeSecurity]
    internal unsafe class Interop32 {

        private const string SOXRLIB = "soxr.x86.dll";
        private const CallingConvention CC = CallingConvention.Cdecl;

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern StringPtr soxr_version();

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrInstance soxr_create(double input_rate, double output_rate, uint num_channels,
            out SoxrError error, SoxrIoSpec io_spec, SoxrQualitySpec quality_spec, SoxrRuntimeSpec runtime_spec);
        
        //[DllImport(SOXRLIB, CallingConvention = CC)]
        //public static extern SoxrError soxr_process(SoxrInstance resampler, 
        //    byte *input, uint ilen, out uint idone,
        //    byte *output, uint olen, out uint odone);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrError soxr_error(SoxrInstance resampler);

        //[DllImport(SOXRLIB, CallingConvention = CC)]
        //public static extern double soxr_delay(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern StringPtr soxr_engine(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern SoxrError soxr_clear(SoxrInstance resampler);

        [DllImport(SOXRLIB, CallingConvention = CC)]
        public static extern void soxr_delete(SoxrInstance resampler);

        //[DllImport(SOXRLIB, CallingConvention = CC)]
        //public static extern SoxrError soxr_set_io_ratio(SoxrInstance resampler, double io_ratio, uint slew_len);

        //[DllImport(SOXRLIB, CallingConvention = CC)]
        //public static extern SoxrQualitySpec soxr_quality_spec(QualityRecipe recipe, QualityFlags flags);

        //[DllImport(SOXRLIB, CallingConvention = CC)]
        //public static extern SoxrRuntimeSpec soxr_runtime_spec(uint num_threads);

        //[DllImport(SOXRLIB, CallingConvention = CC)]
        //public static extern SoxrIoSpec soxr_io_spec(Datatype itype, Datatype otype);
    }
}
