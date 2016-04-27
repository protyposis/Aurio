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
    /// <summary>
    /// Wraps the x86 and x64 interop functions and provides the correct ones depending on the execution platform.
    /// </summary>
    internal class InteropWrapper {

        public const CallingConvention CC = CallingConvention.Cdecl;

        // It would be cleaner/shorter to use Func<> pointers to save the delegate definitions, 
        // but they are not defined for out parameters
        // http://stackoverflow.com/a/20560385

        [UnmanagedFunctionPointer(CC)]
        public delegate int CallbackDelegateReadPacket(IntPtr opaque, IntPtr buffer, int bufferSize);

        [UnmanagedFunctionPointer(CC)]
        public delegate long CallbackDelegateSeek(IntPtr opaque, [MarshalAs(UnmanagedType.I8)] long offset, int whence);

        public delegate IntPtr d_stream_open_file(Type mode, string filename);
        public delegate IntPtr d_stream_open_bufferedio(Type mode, IntPtr opaque, CallbackDelegateReadPacket readPacket, CallbackDelegateSeek seek, string filename);
        public delegate IntPtr d_stream_get_output_config(IntPtr instance, Type type);
        public delegate int d_stream_read_frame(IntPtr instance, out long timestamp, byte[] output_buffer, int output_buffer_size, out int frame_type);
        public delegate void d_stream_seek(IntPtr instance, long timestamp, Type type);
        public delegate void d_stream_seekindex_create(IntPtr instance, Type type);
        public delegate void d_stream_seekindex_remove(IntPtr instance, Type type);
        public delegate void d_stream_close(IntPtr instance);

        public static d_stream_open_file stream_open_file;
        public static d_stream_open_bufferedio stream_open_bufferedio;
        public static d_stream_get_output_config stream_get_output_config;
        public static d_stream_read_frame stream_read_frame;
        public static d_stream_seek stream_seek;
        public static d_stream_seekindex_create stream_seekindex_create;
        public static d_stream_seekindex_remove stream_seekindex_remove;
        public static d_stream_close stream_close;

        static InteropWrapper() {
            if (Environment.Is64BitProcess) {
                stream_open_file = Interop64.stream_open_file;
                stream_open_bufferedio = Interop64.stream_open_bufferedio;
                stream_get_output_config = Interop64.stream_get_output_config;
                stream_read_frame = Interop64.stream_read_frame;
                stream_seek = Interop64.stream_seek;
                stream_seekindex_create = Interop64.stream_seekindex_create;
                stream_seekindex_remove = Interop64.stream_seekindex_remove;
                stream_close = Interop64.stream_close;
            }
            else {
                stream_open_file = Interop32.stream_open_file;
                stream_open_bufferedio = Interop32.stream_open_bufferedio;
                stream_get_output_config = Interop32.stream_get_output_config;
                stream_read_frame = Interop32.stream_read_frame;
                stream_seek = Interop32.stream_seek;
                stream_seekindex_create = Interop32.stream_seekindex_create;
                stream_seekindex_remove = Interop32.stream_seekindex_remove;
                stream_close = Interop32.stream_close;
            }
        }
    }
}
