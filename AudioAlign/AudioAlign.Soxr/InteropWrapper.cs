using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StringPtr = System.IntPtr;
using SoxrInstance = System.IntPtr;
using SoxrError = System.IntPtr;
using SoxrIoSpec = System.IntPtr;
using SoxrQualitySpec = System.IntPtr;
using SoxrRuntimeSpec = System.IntPtr;

namespace AudioAlign.Soxr {
    internal unsafe class InteropWrapper {

        public delegate StringPtr d_soxr_version();
        public delegate SoxrInstance d_soxr_create(double input_rate, double output_rate, uint num_channels,
            out SoxrError error, SoxrIoSpec io_spec, SoxrQualitySpec quality_spec, SoxrRuntimeSpec runtime_spec);
        //public delegate SoxrError d_soxr_process(SoxrInstance resampler,
        //    byte* input, uint ilen, out uint idone,
        //    byte* output, uint olen, out uint odone);
        public delegate SoxrError d_soxr_error(SoxrInstance resampler);
        //public delegate double d_soxr_delay(SoxrInstance resampler);
        public delegate StringPtr d_soxr_engine(SoxrInstance resampler);
        //public delegate SoxrError d_soxr_clear(SoxrInstance resampler);
        //public delegate void d_soxr_delete(SoxrInstance resampler);
        //public delegate SoxrError d_soxr_set_io_ratio(SoxrInstance resampler, double io_ratio, uint slew_len);
        //public delegate SoxrQualitySpec d_soxr_quality_spec(QualityRecipe recipe, QualityFlags flags);
        //public delegate SoxrRuntimeSpec d_soxr_runtime_spec(uint num_threads);
        //public delegate SoxrIoSpec d_soxr_io_spec(Datatype itype, Datatype otype);

        public static d_soxr_version soxr_version;
        public static d_soxr_create soxr_create;
        //public static d_soxr_process soxr_process;
        public static d_soxr_error soxr_error;
        //public static d_soxr_delay soxr_delay;
        public static d_soxr_engine soxr_engine;
        //public static d_soxr_clear soxr_clear;
        //public static d_soxr_delete soxr_delete;
        //public static d_soxr_set_io_ratio soxr_set_io_ratio;
        //public static d_soxr_quality_spec soxr_quality_spec;
        //public static d_soxr_runtime_spec soxr_runtime_spec;
        //public static d_soxr_io_spec soxr_io_spec;

        static InteropWrapper() {
            if (Environment.Is64BitProcess) {
                soxr_version = Interop64.soxr_version;
                soxr_create = Interop64.soxr_create;
                //soxr_process = Interop64.soxr_process;
                soxr_error = Interop64.soxr_error;
                //soxr_delay = Interop64.soxr_delay;
                soxr_engine = Interop64.soxr_engine;
                //soxr_clear = Interop64.soxr_clear;
                //soxr_delete = Interop64.soxr_delete;
                //soxr_set_io_ratio = Interop64.soxr_set_io_ratio;
                //soxr_quality_spec = Interop64.soxr_quality_spec;
                //soxr_runtime_spec = Interop64.soxr_runtime_spec;
                //soxr_io_spec = Interop64.soxr_io_spec;
            }
            else {
                soxr_version = Interop32.soxr_version;
                soxr_create = Interop32.soxr_create;
                //soxr_process = Interop32.soxr_process;
                soxr_error = Interop32.soxr_error;
                //soxr_delay = Interop32.soxr_delay;
                soxr_engine = Interop32.soxr_engine;
                //soxr_clear = Interop32.soxr_clear;
                //soxr_delete = Interop32.soxr_delete;
                //soxr_set_io_ratio = Interop32.soxr_set_io_ratio;
                //soxr_quality_spec = Interop32.soxr_quality_spec;
                //soxr_runtime_spec = Interop32.soxr_runtime_spec;
                //soxr_io_spec = Interop32.soxr_io_spec;
            }
        }
    }
}
