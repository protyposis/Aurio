using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.FFmpeg {
    internal interface IInteropWrapper {
        IntPtr stream_open(string filename);
        IntPtr stream_get_output_config(IntPtr instance);
        int stream_read_frame(IntPtr instance, out long timestamp, out IntPtr data);
        void stream_seek(IntPtr instance, long timestamp);
        void stream_close(IntPtr instance);
    }
}
