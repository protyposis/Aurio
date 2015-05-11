using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace AudioAlign.LibSampleRate {
    internal class Interop32 {

        private const string LIBSAMPLERATE = "libsamplerate-0.dll";
        private const CallingConvention CALLINGCONVENTION = CallingConvention.Cdecl;

        /// Return Type: SRC_STATE*
        /// converter_type: int
        /// channels: int
        /// error: int*
        ///
        /// Standard initialisation function : return an anonymous pointer to the
        ///	internal state of the converter. Choose a converter from the enums below.
        ///	Error returned in *error.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern IntPtr src_new(ConverterType converter_type, int channels, out int error);

        /// Return Type: SRC_STATE*
        /// state: SRC_STATE*
        ///
        /// Cleanup all internal allocations.
        ///	Always returns NULL.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern IntPtr src_delete(IntPtr state);

        /// Return Type: int
        /// state: SRC_STATE*
        /// data: SRC_DATA*
        ///
        /// Standard processing function.
        ///	Returns non zero on error.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern int src_process(IntPtr state, ref SRC_DATA data);

        /// Return Type: int
        /// state: SRC_STATE*
        ///
        /// Reset the internal SRC state.
        /// Does not modify the quality settings.
        /// Does not free any memory allocations.
        /// Returns non zero on error.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern int src_reset(IntPtr state);

        /// Return Type: int
        /// state: SRC_STATE*
        /// new_ratio: double
        ///
        /// The src_set_ratio function allows the modification of the conversion ratio between 
        /// calls to src_process. This allows a step response in the conversion ratio. It returns 
        /// non-zero on error and the error return value can be decoded into a text string.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern int src_set_ratio(IntPtr state, double new_ratio);

        /// Return Type: int
        ///ratio: double
        ///
        /// Set a new SRC ratio. This allows step responses in the conversion ratio.
        ///	Returns non zero on error.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern int src_is_valid_ratio(double ratio);

        /// Return Type: int
        /// state: SRC_STATE*
        ///
        /// Return an error number.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern int src_error(IntPtr state);

        /// Return Type: char*
        /// error: int
        ///
        /// Convert the error number into a string.
        [SuppressUnmanagedCodeSecurity]
        [DllImport(LIBSAMPLERATE, CallingConvention = CALLINGCONVENTION)]
        public static extern string src_strerror(int error);
    }
}
