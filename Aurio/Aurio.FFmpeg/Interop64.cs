// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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
using System.Runtime.InteropServices;
using System.Text;

namespace Aurio.FFmpeg {
    internal class Interop64 {

        private const string FFMPEGPROXYLIB = "ffmpeg64\\Aurio.FFmpeg.Proxy64.dll";

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern IntPtr stream_open_file(string filename);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern IntPtr stream_open_bufferedio(IntPtr opaque, InteropWrapper.CallbackDelegateReadPacket readPacket, InteropWrapper.CallbackDelegateSeek seek);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern IntPtr stream_get_output_config(IntPtr instance);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern int stream_read_frame(IntPtr instance, out long timestamp, byte[] output_buffer, int output_buffer_size);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern void stream_seek(IntPtr instance, long timestamp);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern void stream_close(IntPtr instance);
    }
}
