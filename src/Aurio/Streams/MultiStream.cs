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
using System.Linq;
using System.Text;

namespace Aurio.Streams
{
    /// <summary>
    /// Combines/concatenates multiple files or streams into a single stream.
    /// </summary>
    public class MultiStream : Stream
    {
        private readonly Stream[] subStreams;

        private int currentSubStreamIndex;

        private bool canRead;
        private bool canWrite;
        private bool canSeek;

        public MultiStream(params Stream[] subStreams)
        {
            this.subStreams = subStreams;

            canRead = canWrite = canSeek = true;
            foreach (Stream stream in subStreams)
            {
                if (!stream.CanRead)
                {
                    canRead = false;
                }
                if (!stream.CanWrite)
                {
                    canWrite = false;
                }
                if (!stream.CanSeek)
                {
                    canSeek = false;
                }
            }

            Console.WriteLine(
                "multifile stream initialized ["
                    + subStreams.Length
                    + " files, "
                    + Length
                    + " bytes]"
            );
            CurrentSubStreamIndex = 0;
        }

        public static MultiStream FromFileInfos(params FileInfo[] files)
        {
            Stream[] subStreams = new FileStream[files.Length];

            for (int x = 0; x < files.Length; x++)
            {
                Console.WriteLine("multifile stream #" + x + ": " + files[x].FullName);
                subStreams[x] = files[x].OpenRead();
            }

            return new MultiStream(subStreams);
        }

        public override bool CanRead
        {
            get { return canRead; }
        }

        public override bool CanSeek
        {
            get { return canSeek; }
        }

        public override bool CanWrite
        {
            get { return canWrite; }
        }

        public override void Flush()
        {
            CurrentSubStream.Flush();
        }

        public override long Length
        {
            get
            {
                long length = 0;
                foreach (Stream fs in subStreams)
                {
                    length += fs.Length;
                }
                return length;
            }
        }

        public override long Position
        {
            get
            {
                long pos = 0;
                for (int x = 0; x < currentSubStreamIndex; x++)
                {
                    pos += subStreams[x].Length;
                }
                return pos + subStreams[currentSubStreamIndex].Position;
            }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = CurrentSubStream.Read(buffer, offset, count);

            if (bytesRead == 0 && count > 0)
            {
                // we reached the EOF
                Console.WriteLine("end of substream " + currentSubStreamIndex + " reached");

                if (LastSubStream)
                {
                    // we reached not only EOF, but EOF of the last stream (so we're at the very end)
                    Console.WriteLine("end of stream reached");
                    return bytesRead;
                }

                // switch to and continue with next file stream
                CurrentSubStreamIndex++;

                return Read(buffer, offset, count);
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }

            Console.WriteLine("seeking from origin " + origin + " to offset " + offset);

            long newPos = 0;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPos = offset;
                    break;
                case SeekOrigin.Current:
                    newPos = Position + offset;
                    break;
                case SeekOrigin.End:
                    newPos = Length + offset;
                    break;
            }

            long streamLengthSum = 0;
            int streamIndex = 0;

            while (streamLengthSum + subStreams[streamIndex].Length < newPos)
            {
                streamLengthSum += subStreams[streamIndex].Length;
                streamIndex++;
            }

            if (CurrentSubStreamIndex != streamIndex)
            {
                CurrentSubStreamIndex = streamIndex;
            }
            CurrentSubStream.Seek(newPos - streamLengthSum, SeekOrigin.Begin);

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            new List<Stream>(subStreams).ForEach(s => s.Close());
        }

        private bool FirstSubStream
        {
            get { return currentSubStreamIndex == 0; }
        }

        private bool LastSubStream
        {
            get { return currentSubStreamIndex == subStreams.Length - 1; }
        }

        private Stream CurrentSubStream
        {
            get { return subStreams[currentSubStreamIndex]; }
        }

        private int CurrentSubStreamIndex
        {
            get { return currentSubStreamIndex; }
            set
            {
                Console.WriteLine(
                    "substream index change: " + currentSubStreamIndex + " -> " + value
                );
                if (CurrentSubStream != null)
                {
                    CurrentSubStream.Flush();
                }
                currentSubStreamIndex = value;
            }
        }
    }
}
