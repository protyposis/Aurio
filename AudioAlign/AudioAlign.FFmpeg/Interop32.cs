using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioAlign.FFmpeg {
    internal class Interop32 {

        private const string FFMPEGPROXYLIB = "ffmpeg32\\AudioAlign.FFmpeg.Proxy32.dll";
        private const CallingConvention CC = CallingConvention.Cdecl;

        [DllImport(FFMPEGPROXYLIB, CallingConvention = CC)]
        public static extern IntPtr stream_open(string filename);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = CC)]
        public static extern IntPtr stream_get_output_config(IntPtr instance);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = CC)]
        public static extern int stream_read_frame(IntPtr instance, out long timestamp, byte[] output_buffer, int output_buffer_size);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = CC)]
        public static extern void stream_seek(IntPtr instance, long timestamp);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = CC)]
        public static extern void stream_close(IntPtr instance);
    }
}
