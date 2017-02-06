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
using System.Runtime.InteropServices;
using System.Security;

namespace Aurio.LibSampleRate {
    internal class Interop64 {

        private const string LIBSAMPLERATE = "libsamplerate-0.x64.dll";
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
