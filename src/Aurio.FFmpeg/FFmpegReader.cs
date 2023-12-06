//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
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
using System.IO;
using System.Runtime.InteropServices;

namespace Aurio.FFmpeg
{
    public class FFmpegReader : IDisposable
    {
        private string filename; // store source filename for debugging
        private bool disposed = false;
        private Type mode;
        private IntPtr instance = IntPtr.Zero;
        private AudioOutputConfig audioOutputConfig;
        private VideoOutputConfig videoOutputConfig;

        // Delegates for buffered IO mode (stream source)
        // Because the CLR does not know about the references from the native proxy code,
        // They must be stored in class fields to keep a reference and prevent them from being collected.
        private InteropWrapper.CallbackDelegateReadPacket readPacketDelegate;
        private InteropWrapper.CallbackDelegateSeek seekDelegate;

        /// <summary>
        /// Instatiates an FFmpeg reader that works in file mode, where FFmpeg gets the file name and
        /// handles file access itself.
        /// </summary>
        /// <param name="filename">the name of the file to read</param>
        /// <param name="mode">the types of data to read</param>
        public FFmpegReader(string filename, Type mode)
        {
            this.filename = filename;
            this.mode = mode;

            instance = InteropWrapper.stream_open_file(mode, filename);

            CheckAndHandleOpeningError();

            ReadOutputConfig();
        }

        /// <summary>
        /// Instatiates an FFmpeg reader that works in file mode, where FFmpeg gets the file name and
        /// handles file access itself.
        /// </summary>
        /// <param name="fileInfo">a FileInfo object of the file to read</param>
        /// <param name="mode">the types of data to read</param>
        public FFmpegReader(FileInfo fileInfo, Type mode)
            : this(fileInfo.FullName, mode) { }

        /// <summary>
        /// Instantiates an FFmpeg reader in stream mode, where FFmpeg only gets stream reading callbacks
        /// and the actual file access is handled by the caller. An optional file name hint can be passed
        /// to FFmpeg to help it detect the file format, which is useful for file formats without
        /// distinct headers (e.g. SHN).
        /// </summary>
        /// <param name="stream">the stream to decode</param>
        /// <param name="mode">the types of data to read</param>
        /// <param name="fileName">optional filename as a hint for FFmpeg to determine the data format</param>
        public FFmpegReader(Stream stream, Type mode, string fileName)
        {
            this.filename = fileName ?? "bufferedIO_stream";
            this.mode = mode;

            var transferBuffer = new byte[0];
            readPacketDelegate = delegate(IntPtr opaque, IntPtr buffer, int bufferSize)
            {
                /* NOTE there's no way to cast the IntPtr to a byte array which is required
                 * for stream reading, so we need to add an intermediary transfer buffer.
                 */
                // Increase transfer buffer's size if too small
                if (transferBuffer.Length < bufferSize)
                {
                    transferBuffer = new byte[bufferSize];
                }
                // Read data into transfer buffer
                int bytesRead = stream.Read(transferBuffer, 0, bufferSize);

                // Transfer data to unmanaged memory
                Marshal.Copy(transferBuffer, 0, buffer, bytesRead);

                // Return number of bytes read
                return bytesRead;
            };
            seekDelegate = delegate(IntPtr opaque, long offset, int whence)
            {
                if (
                    whence == 0x10000 /* AVSEEK_SIZE */
                )
                {
                    return stream.Length;
                }
                return stream.Seek(offset, (SeekOrigin)whence);
            };

            instance = InteropWrapper.stream_open_bufferedio(
                mode,
                IntPtr.Zero,
                readPacketDelegate,
                seekDelegate,
                fileName
            );

            CheckAndHandleOpeningError();

            ReadOutputConfig();
        }

        /// <summary>
        /// Instantiates an FFmpeg reader in stream mode, where FFmpeg only gets stream reading callbacks
        /// and the actual file access is handled by the caller.
        /// </summary>
        /// <param name="stream">the stream to decode</param>
        /// <param name="mode">the types of data to read</param>
        public FFmpegReader(Stream stream, Type mode)
            : this(stream, mode, null) { }

        private void CheckAndHandleOpeningError()
        {
            if (InteropWrapper.stream_has_error(instance))
            {
                string errorMessage = Marshal.PtrToStringAnsi(
                    InteropWrapper.stream_get_error(instance)
                );
                throw new IOException("Error opening the FFmpeg stream: " + errorMessage);
            }
        }

        private void CheckAndHandleActiveInstance()
        {
            if (disposed)
            {
                throw new IOException("Cannot operate on a disposed stream");
            }
        }

        private void ReadOutputConfig()
        {
            if ((mode & Type.Audio) != 0)
            {
                IntPtr ocp = InteropWrapper.stream_get_output_config(instance, Type.Audio);
                audioOutputConfig = (AudioOutputConfig)
                    Marshal.PtrToStructure(ocp, typeof(AudioOutputConfig));
            }

            if ((mode & Type.Video) != 0)
            {
                IntPtr ocp = InteropWrapper.stream_get_output_config(instance, Type.Video);
                videoOutputConfig = (VideoOutputConfig)
                    Marshal.PtrToStructure(ocp, typeof(VideoOutputConfig));
            }
        }

        public AudioOutputConfig AudioOutputConfig
        {
            get { return audioOutputConfig; }
        }

        public VideoOutputConfig VideoOutputConfig
        {
            get { return videoOutputConfig; }
        }

        public int ReadFrame(
            out long timestamp,
            byte[] output_buffer,
            int output_buffer_size,
            out Type frameType
        )
        {
            CheckAndHandleActiveInstance();

            int ret = InteropWrapper.stream_read_frame(
                instance,
                out timestamp,
                output_buffer,
                output_buffer_size,
                out int type
            );
            frameType = (Type)type;

            return ret;
        }

        public void Seek(long timestamp, Type type)
        {
            CheckAndHandleActiveInstance();
            InteropWrapper.stream_seek(instance, timestamp, type);
        }

        public void CreateSeekIndex(Type type)
        {
            CheckAndHandleActiveInstance();
            InteropWrapper.stream_seekindex_create(instance, type);
        }

        public void RemoveSeekIndex(Type type)
        {
            CheckAndHandleActiveInstance();
            InteropWrapper.stream_seekindex_remove(instance, type);
        }

        #region IDisposable & destructor

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// http://www.codeproject.com/KB/cs/idisposable.aspx
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
                if (instance != IntPtr.Zero)
                {
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

        ~FFmpegReader()
        {
            Dispose(false);
        }

        #endregion
    }
}
