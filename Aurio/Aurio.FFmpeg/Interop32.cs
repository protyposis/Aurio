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
    internal class Interop32 {

        private const string FFMPEGPROXYLIB = "ffmpeg32\\Aurio.FFmpeg.Proxy32.dll";

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern IntPtr stream_open_file(Type mode, string filename);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern IntPtr stream_open_bufferedio(Type mode, IntPtr opaque, InteropWrapper.CallbackDelegateReadPacket readPacket, InteropWrapper.CallbackDelegateSeek seek, string filename);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern IntPtr stream_get_output_config(IntPtr instance, Type type);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern int stream_read_frame(IntPtr instance, out long timestamp, byte[] output_buffer, int output_buffer_size, out int frame_type);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern void stream_seek(IntPtr instance, long timestamp, Type type);

        [DllImport(FFMPEGPROXYLIB, CallingConvention = InteropWrapper.CC)]
        public static extern void stream_close(IntPtr instance);
    }
}
