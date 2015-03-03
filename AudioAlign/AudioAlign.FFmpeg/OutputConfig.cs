using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioAlign.FFmpeg {

    [StructLayout(LayoutKind.Sequential)]
    public struct OutputFormat {
        int sample_rate;
        int sample_size;
        int channels;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OutputConfig {
        OutputFormat format;
        long length;
        int frame_size;
    }
}
