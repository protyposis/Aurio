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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Aurio.Streams;
using NAudio.Wave;

namespace Aurio.FFmpeg
{
    public class FFmpegSourceStream : IAudioStream
    {
        public const string ProxyFileExtension = ".ffproxy.wav";

        private Stream sourceStream;
        private FFmpegReader reader;
        private AudioProperties properties;
        private long readerPosition; // samples
        private long readerFirstPTS; // samples

        private byte[] sourceBuffer;
        private int sourceBufferLength; // samples
        private int sourceBufferPosition; // samples

        private bool seekIndexCreated;

        /// <summary>
        /// Decodes an audio stream through FFmpeg from an encoded file.
        /// </summary>
        /// <param name="fileInfo">the file to decode</param>
        public FFmpegSourceStream(FileInfo fileInfo)
            : this(fileInfo.OpenRead(), fileInfo.Name)
        {
            //reader = new FFmpegReader(fileInfo); // use filesystem IO
            //reader = new FFmpegReader(fileInfo.OpenRead()); // use buffered IO with stream
        }

        /// <summary>
        /// Decodes an audio stream through FFmpeg.
        /// </summary>
        /// <param name="reader">an FFmpeg reader of an encoded stream</param>
        public FFmpegSourceStream(FFmpegReader reader)
        {
            this.reader = reader;

            if (reader.AudioOutputConfig.length == long.MinValue)
            {
                /*
                 * length == FFmpeg AV_NOPTS_VALUE
                 *
                 * This means that for the opened file/format, there is no length/PTS data
                 * available, which also makes seeking more or less impossible.
                 *
                 * As a workaround, an index could be created to map the frames to the file
                 * position, and then seek by file position. The index could be created by
                 * linearly reading through the file (decoding not necessary), and creating
                 * a mapping of AVPacket.pos to the frame time.
                 */
                throw new FileNotSeekableException();
            }

            properties = new AudioProperties(
                reader.AudioOutputConfig.format.channels,
                reader.AudioOutputConfig.format.sample_rate,
                reader.AudioOutputConfig.format.sample_size * 8,
                reader.AudioOutputConfig.format.sample_size == 4
                    ? AudioFormat.IEEE
                    : AudioFormat.LPCM
            );

            readerPosition = 0;
            sourceBuffer = new byte[reader.FrameBufferSize];
            sourceBufferPosition = 0;
            sourceBufferLength = -1; // -1 means buffer empty, >= 0 means valid buffer data

            DetermineFirstPts();

            seekIndexCreated = false;
        }

        /// <summary>
        /// Decodes an audio stream through FFmpeg from an encoded file stream.
        /// Accepts an optional file name hint to help FFmpeg determine the format of
        /// the encoded data.
        /// </summary>
        /// <param name="stream">the stream to decode</param>
        /// <param name="fileName">optional file name hint for FFmpeg</param>
        public FFmpegSourceStream(Stream stream, string fileName)
            : this(new FFmpegReader(stream, FFmpeg.Type.Audio, fileName))
        {
            sourceStream = stream;
        }

        /// <summary>
        /// Decodes an audio stream through FFmpeg from an encoded file stream.
        /// </summary>
        /// <param name="stream">the stream to decode</param>
        public FFmpegSourceStream(Stream stream)
            : this(stream, null) { }

        public AudioProperties Properties
        {
            get { return properties; }
        }

        public long Length
        {
            get { return reader.AudioOutputConfig.length * properties.SampleBlockByteSize; }
        }

        private long SamplePosition
        {
            get { return readerPosition + sourceBufferPosition - readerFirstPTS; }
        }

        private void ReadFrame()
        {
            sourceBufferLength = reader.ReadFrame(
                out readerPosition,
                sourceBuffer,
                sourceBuffer.Length,
                out Type type
            );
        }

        private void DetermineFirstPts()
        {
            // Determine first PTS to handle files which don't start at zero. This is
            // important for seeking and reporting the proper stream length, because
            // Aurio streams always start at zero.
            reader.Seek(long.MinValue, Type.Audio);
            ReadFrame();

            if (sourceBufferLength < 0)
            {
                // If determining the first PTS does not work, we have to expect other
                // seeks to fail too.
                throw new FileNotSeekableException("Determining first PTS failed");
            }

            readerFirstPTS = readerPosition;
            Console.WriteLine("first PTS = " + readerFirstPTS);
        }

        /// <summary>
        /// Read frames (repeatedly) into the buffer until it contains the sample with the
        /// desired timestamp.
        ///
        /// This is a helper method for sample-exact seeking, because FFmpeg seeks may end
        /// up a long way before the desired seek target.
        /// </summary>
        private void ForwardReadUntilTimestamp(long targetTimestamp)
        {
            long previousReaderPosition = long.MinValue;

            while (true)
            {
                ReadFrame();

                if (readerPosition == -1)
                {
                    // "Timestamp not found (end of file reached)"
                    return;
                }
                else if (readerPosition == previousReaderPosition)
                {
                    // Prevent an endless read-loop in case the reported position does not change.
                    // I did not encounter this behavior, but who knows how FFmpeg acts on the myriad of supported formats.
                    throw new InvalidOperationException("Read head is stuck");
                }
                else if (targetTimestamp < readerPosition)
                {
                    // Prevent scanning to end of stream in case the target timestamp gets skipped.
                    throw new SeekTargetMissedException(
                        "Read position is beyond the target timestamp"
                    );
                }
                else if (targetTimestamp < readerPosition + sourceBufferLength)
                {
                    break;
                }

                previousReaderPosition = readerPosition;
            }
        }

        public long Position
        {
            get { return SamplePosition * SampleBlockSize; }
            set
            {
                long seekTarget = (value / SampleBlockSize) + readerFirstPTS;

                // seek to target position
                try
                {
                    reader.Seek(seekTarget, Type.Audio);
                    ForwardReadUntilTimestamp(seekTarget);
                }
                catch (SeekTargetMissedException)
                {
                    // FFmpeg seek is sometimes off by one frame. Try again, this time
                    // by seeking to the previous frame.
                    // This is an issue with the FFmpeg seek function, see `stream_seek` in `proxy.c`
                    // for details.
                    Console.WriteLine("Seek target miss. Retry seeking to earlier position.");
                    reader.Seek(seekTarget - reader.AudioOutputConfig.frame_size, Type.Audio);
                    ForwardReadUntilTimestamp(seekTarget);
                }

                if (sourceBufferLength == -1)
                {
                    // End of stream handling:
                    // The Aurio stream interface allows seeking beyond the end, so we act
                    // like the seek was successful. `Read` won't return any samples though.
                    readerPosition = seekTarget;
                    sourceBufferPosition = 0;
                    return;
                }

                // check if seek ended up at seek target (or earlier because of frame size, depends on file format and stream codec)
                if (seekTarget == readerPosition)
                {
                    // perfect case
                    sourceBufferPosition = 0;
                }
                else if (
                    seekTarget > readerPosition
                    && seekTarget <= (readerPosition + sourceBufferLength)
                )
                {
                    sourceBufferPosition = (int)(seekTarget - readerPosition);
                }
                else if (seekTarget < readerPosition)
                {
                    throw new InvalidOperationException("illegal state");
                }

                if (Position != value)
                {
                    // When seeking fails by ending up at another position than the requested position, we create a seek index
                    // to support the seeking process which takes some time but hopefully solves the problem. If this does not
                    // solve the problem and it happens again, we throw an exception because we cannot count on this stream.
                    if (!seekIndexCreated)
                    {
                        reader.CreateSeekIndex(FFmpeg.Type.Audio);
                        seekIndexCreated = true;
                        Console.WriteLine("seek index created");

                        // With the seek index, try seeking again.
                        // This does not result in an endless recursion because of the seekIndexCreated flag.
                        Position = value;
                    }
                    else
                    {
                        throw new FileSeekException(
                            String.Format(
                                "seeking did not work correctly: expected {0}, result {1}",
                                value,
                                Position
                            )
                        );
                    }
                }
            }
        }

        public int SampleBlockSize
        {
            get { return properties.SampleBlockByteSize; }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (sourceBufferLength == -1)
            {
                sourceBufferLength = reader.ReadFrame(
                    out long newPosition,
                    sourceBuffer,
                    sourceBuffer.Length,
                    out Type type
                );

                if (sourceBufferLength == -1)
                {
                    return 0; // end of stream
                }

                readerPosition = newPosition;
                sourceBufferPosition = 0;
            }

            int bytesToCopy = Math.Min(
                count,
                (sourceBufferLength - sourceBufferPosition) * SampleBlockSize
            );
            Array.Copy(
                sourceBuffer,
                sourceBufferPosition * SampleBlockSize,
                buffer,
                offset,
                bytesToCopy
            );
            sourceBufferPosition += (bytesToCopy / SampleBlockSize);
            if (sourceBufferPosition > sourceBufferLength)
            {
                throw new InvalidOperationException("overflow");
            }
            else if (sourceBufferPosition == sourceBufferLength)
            {
                // buffer read completely, need to read next frame
                sourceBufferLength = -1;
            }

            return bytesToCopy;
        }

        public void Close()
        {
            reader.Dispose();
            if (sourceStream != null)
            {
                sourceStream.Close();
                sourceStream = null;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public static FileInfo CreateWaveProxy(FileInfo fileInfo, FileInfo proxyFileInfo)
        {
            using var fileStream = fileInfo.OpenRead();
            return CreateWaveProxy(fileStream, proxyFileInfo);
        }

        public static FileInfo CreateWaveProxy(Stream fileStream, FileInfo proxyFileInfo)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (proxyFileInfo == null)
            {
                throw new ArgumentNullException(nameof(proxyFileInfo));
            }

            if (proxyFileInfo.Exists)
            {
                Console.WriteLine("Proxy already existing, using " + proxyFileInfo.Name);
                return proxyFileInfo;
            }

            // Use a temporary file during writing to avoid incomplete proxy files on unexpected termination
            var tempProxyFileInfo = new FileInfo(proxyFileInfo.FullName + ".part");

            using (var proxyStream = tempProxyFileInfo.OpenWrite())
            {
                CreateWaveProxy(fileStream, proxyStream);
            }

            // Move temp file to final proxy file
            tempProxyFileInfo.MoveTo(proxyFileInfo.FullName, true);

            return proxyFileInfo;
        }

        public static void CreateWaveProxy(Stream fileStream, Stream proxyStream)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (proxyStream == null)
            {
                throw new ArgumentNullException(nameof(proxyStream));
            }

            var reader = new FFmpegReader(fileStream, FFmpeg.Type.Audio);
            var properties = new AudioProperties(
                reader.AudioOutputConfig.format.channels,
                reader.AudioOutputConfig.format.sample_rate,
                reader.AudioOutputConfig.format.sample_size * 8,
                reader.AudioOutputConfig.format.sample_size == 4
                    ? AudioFormat.IEEE
                    : AudioFormat.LPCM
            );

            var dummyStream = new NAudioSinkStream(new NullStream(properties, 0));
            var waveFileWriterFormat = dummyStream.WaveFormat;

            using WaveFileWriter waveFileWriter = new WaveFileWriter(
                proxyStream,
                waveFileWriterFormat
            );

            int output_buffer_size = reader.AudioOutputConfig.frame_size * dummyStream.BlockAlign;
            byte[] output_buffer = new byte[output_buffer_size];

            int samplesRead;

            // sequentially read samples from decoder and write it to wav file
            while (
                (
                    samplesRead = reader.ReadFrame(
                        out long timestamp,
                        output_buffer,
                        output_buffer_size,
                        out Type type
                    )
                ) > 0
            )
            {
                int bytesRead = samplesRead * dummyStream.BlockAlign;
                waveFileWriter.Write(output_buffer, 0, bytesRead);
            }

            waveFileWriter.Flush();
        }

        /// <summary>
        /// Creates a Wave format proxy file for the given file and optional directory according
        /// to <see cref="SuggestWaveProxyFileInfo(FileInfo, DirectoryInfo)"/>.
        /// </summary>
        /// <param name="fileInfo">the file for which a proxy file should be created</param>
        /// <param name="storageDirectory">optional directory where the proxy file will be stored, can be null</param>
        /// <returns>the FileInfo of the proxy file</returns>
        public static FileInfo CreateWaveProxy(FileInfo fileInfo, DirectoryInfo storageDirectory)
        {
            var proxyFileInfo = SuggestWaveProxyFileInfo(fileInfo, storageDirectory);
            return CreateWaveProxy(fileInfo, proxyFileInfo);
        }

        /// <summary>
        /// Creates a Wave format proxy file in the same directory and with the same name as the specified file.
        /// </summary>
        /// <param name="fileInfo">the file for which a proxy file will be created</param>
        /// <returns>the FileInfo of the proxy file</returns>
        public static FileInfo CreateWaveProxy(FileInfo fileInfo)
        {
            return CreateWaveProxy(fileInfo, (DirectoryInfo)null);
        }

        /// <summary>
        /// Checks for a file if it is recommended to build a wave proxy.. A proxy is recommended if the file
        /// format does not support seeking or is known to have seek issues.
        /// An alternative is to scan the file and build a seek index, but this is not implemented yet.
        /// </summary>
        /// <param name="fileInfo">the file for which to check if a proxy is recommended</param>
        /// <returns>true is a proxy is recommended, else false</returns>
        public static bool WaveProxySuggested(FileInfo fileInfo)
        {
            // Suggest wave proxy for file formats with known seek issues
            return new List<string>() { ".shn", ".ape" }.Exists(
                ext => fileInfo.Extension.ToLowerInvariant().Equals(ext)
            );
        }

        /// <summary>
        /// Creates a proxy file info for the provided file with the <see cref="ProxyFileExtension"/>.
        ///
        /// If a storage directory is specified, the proxy file will be located in the specified directory
        /// with a hashed file name to avoid name collisions. This option is is convenient when using
        /// temporary or working directories.
        ///
        /// If no storage diretory is specified, the proxy file will be located in the same directory and
        /// with the same name as the specified file.
        /// </summary>
        /// <param name="fileDescriptor">the file for which a proxy file should be created</param>
        /// <param name="storageDirectory">optional directory where the proxy file will be stored (can be null)</param>
        /// <returns>the FileInfo of the suggested proxy file</returns>
        public static FileInfo SuggestWaveProxyFileInfo(
            FileInfo fileInfo,
            DirectoryInfo storageDirectory = null
        )
        {
            return SuggestWaveProxyFileInfo(
                new FileDescriptor(fileInfo.FullName, fileInfo.Length, fileInfo.LastWriteTimeUtc),
                storageDirectory
            );
        }

        public static FileInfo SuggestWaveProxyFileInfo(
            FileDescriptor fileDescriptor,
            DirectoryInfo storageDirectory = null
        )
        {
            if (storageDirectory == null)
            {
                // Without a storage directory, store the proxy file beside the original file
                return new FileInfo(fileDescriptor.FullName + ProxyFileExtension);
            }
            else
            {
                // With a storage directory specified, store the proxy file with a hashed name
                // (to avoid name collision / overwrites) in the target directory (e.g. a temp or working directory)
                var name = SuggestWaveProxyFileName(fileDescriptor);
                return new FileInfo(
                    Path.Combine(storageDirectory.FullName, name + ProxyFileExtension)
                );
            }
        }

        public static string SuggestWaveProxyFileName(FileDescriptor fileDescriptor)
        {
            // Include file size and last write timestamp (UTC) in hash to differentiate identical paths with changed contents.
            string hashInput =
                fileDescriptor.FullName
                + "|"
                + fileDescriptor.Length
                + "|"
                + fileDescriptor.LastWriteTimeUtc.Ticks;

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.Unicode.GetBytes(hashInput));
            string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return hashString;
        }

        public class FileNotSeekableException : Exception
        {
            public FileNotSeekableException()
                : base() { }

            public FileNotSeekableException(string message)
                : base(message) { }

            public FileNotSeekableException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        public class FileSeekException : Exception
        {
            public FileSeekException()
                : base() { }

            public FileSeekException(string message)
                : base(message) { }

            public FileSeekException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        private class SeekTargetMissedException : Exception
        {
            public SeekTargetMissedException()
                : base() { }

            public SeekTargetMissedException(string message)
                : base(message) { }

            public SeekTargetMissedException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        /// <summary>
        /// Lightweight immutable abstraction of <see cref="FileInfo"/> holding only the metadata
        /// required for proxy file name generation.
        /// Use this instead of <see cref="FileInfo"/> where direct filesystem access should be
        /// avoided (e.g. in unit tests) or when a simple value object is sufficient.
        /// </summary>
        /// <param name="FullName">Absolute path (or unique identifier) of the original file.</param>
        /// <param name="Length">File size in bytes.</param>
        /// <param name="LastWriteTimeUtc">Last write time expressed in UTC; used to distinguish changed contents with identical paths and sizes.</param>
        public record FileDescriptor(string FullName, long Length, DateTime LastWriteTimeUtc);
    }
}
