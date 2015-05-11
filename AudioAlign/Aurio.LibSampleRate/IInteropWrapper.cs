using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.LibSampleRate {
    internal interface IInteropWrapper {
        IntPtr src_new(ConverterType converter_type, int channels, out int error);
        IntPtr src_delete(IntPtr state);
        int src_process(IntPtr state, ref SRC_DATA data);
        int src_reset(IntPtr state);
        int src_set_ratio(IntPtr state, double new_ratio);
        int src_is_valid_ratio(double ratio);
        int src_error(IntPtr state);
        string src_strerror(int error);
    }
}
