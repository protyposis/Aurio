using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.LibSampleRate {
    internal class Interop32Wrapper : IInteropWrapper {
        #region IInteropWrapper Members

        public IntPtr src_new(ConverterType converter_type, int channels, out int error) {
            return Interop32.src_new(converter_type, channels, out error);
        }

        public IntPtr src_delete(IntPtr state) {
            return Interop32.src_delete(state);
        }

        public int src_process(IntPtr state, ref SRC_DATA data) {
            return Interop32.src_process(state, ref data);
        }

        public int src_reset(IntPtr state) {
            return Interop32.src_reset(state);
        }

        public int src_set_ratio(IntPtr state, double new_ratio) {
            return Interop32.src_set_ratio(state, new_ratio);
        }

        public int src_is_valid_ratio(double ratio) {
            return Interop32.src_is_valid_ratio(ratio);
        }

        public int src_error(IntPtr state) {
            return Interop32.src_error(state);
        }

        public string src_strerror(int error) {
            return Interop32.src_strerror(error);
        }

        #endregion
    }
}
