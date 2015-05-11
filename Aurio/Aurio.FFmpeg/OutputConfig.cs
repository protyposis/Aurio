using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Aurio.FFmpeg {

    [StructLayout(LayoutKind.Sequential)]
    public struct OutputFormat {
        public int sample_rate { get; internal set; }
        public int sample_size { get; internal set; }
        public int channels { get; internal set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OutputConfig {
        public OutputFormat format { get; internal set; }
        public long length { get; internal set; }
        public int frame_size { get; internal set; }
    }
}
