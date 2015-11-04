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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Aurio.FFmpeg {
    public class FFmpegReader : IDisposable {

        private string filename; // store source filename for debugging
        private bool disposed = false;
        private IntPtr instance = IntPtr.Zero;
        private OutputConfig outputConfig;

        // Delegates for buffered IO mode (stream source)
        // Because the CLR does not know about the references from the native proxy code,
        // They must be stored in class fields to keep a reference and prevent them from being collected.
        private InteropWrapper.CallbackDelegateReadPacket readPacketDelegate;
        private InteropWrapper.CallbackDelegateSeek seekDelegate;

        public FFmpegReader(string filename) {
            this.filename = filename;
            instance = InteropWrapper.stream_open_file(filename);

            IntPtr ocp = InteropWrapper.stream_get_output_config(instance);
            outputConfig = (OutputConfig)Marshal.PtrToStructure(ocp, typeof(OutputConfig));
        }

        public FFmpegReader(FileInfo fileInfo) : this(fileInfo.FullName) { }

        public FFmpegReader(Stream stream) {
            this.filename = "bufferedIO_stream";

            var transferBuffer = new byte[0];
            readPacketDelegate = delegate (IntPtr opaque, IntPtr buffer, int bufferSize) {
                /* NOTE there's no way to cast the IntPtr to a byte array which is required 
                 * for stream reading, so we need to add an intermediary transfer buffer.
                 */
                // Increase transfer buffer's size if too small
                if (transferBuffer.Length < bufferSize) {
                    transferBuffer = new byte[bufferSize];
                }
                // Read data into transfer buffer
                int bytesRead = stream.Read(transferBuffer, 0, bufferSize);

                // Transfer data to unmanaged memory
                Marshal.Copy(transferBuffer, 0, buffer, bytesRead);

                // Return number of bytes read
                return bytesRead;
            };
            seekDelegate = delegate (IntPtr opaque, long offset, int whence) {
                if (whence == 0x10000 /* AVSEEK_SIZE */) {
                    return stream.Length;
                }
                return stream.Seek(offset, (SeekOrigin)whence);
            };

            instance = InteropWrapper.stream_open_bufferedio(IntPtr.Zero, readPacketDelegate, seekDelegate);

            IntPtr ocp = InteropWrapper.stream_get_output_config(instance);
            outputConfig = (OutputConfig)Marshal.PtrToStructure(ocp, typeof(OutputConfig));
        }

        public OutputConfig OutputConfig {
            get { return outputConfig; }
        }

        public int ReadFrame(out long timestamp, byte[] output_buffer, int output_buffer_size) {
            return InteropWrapper.stream_read_frame(instance, out timestamp, output_buffer, output_buffer_size);
        }

        public void Seek(long timestamp) {
            InteropWrapper.stream_seek(instance, timestamp);
        }

        #region IDisposable & destructor

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// http://www.codeproject.com/KB/cs/idisposable.aspx
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    // Dispose managed resources.
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
                if (instance != IntPtr.Zero) {
                    InteropWrapper.stream_close(instance);
                    instance = IntPtr.Zero;
                    readPacketDelegate = null;
                    seekDelegate = null;
                }
            }
            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);
        }

        ~FFmpegReader() {
            Dispose(false);
        }

        #endregion
    }
}
