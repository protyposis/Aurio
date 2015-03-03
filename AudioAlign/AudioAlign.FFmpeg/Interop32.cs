using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioAlign.FFmpeg {
    internal class Interop32 {

        private const string FFMPEGPROXYLIB = "win32\\AudioAlign.FFmpeg.Proxy32.dll";

        [DllImport(FFMPEGPROXYLIB)]
        public static extern IntPtr stream_open(string filename);

        [DllImport(FFMPEGPROXYLIB)]
        public static extern IntPtr stream_get_output_config(IntPtr instance);

        [DllImport(FFMPEGPROXYLIB)]
        public static extern int stream_read_frame(IntPtr instance);

        [DllImport(FFMPEGPROXYLIB)]
        public static extern void stream_close(IntPtr instance);
    }
}
