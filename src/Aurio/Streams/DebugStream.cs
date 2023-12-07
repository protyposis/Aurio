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

using System.Diagnostics;

namespace Aurio.Streams
{
    public class DebugStream : AbstractAudioStreamWrapper
    {
        private long totalBytesRead;
        private long calculatedLength;

        public DebugStream(IAudioStream sourceStream)
            : base(sourceStream)
        {
            totalBytesRead = 0;
            calculatedLength = 0;
        }

        public DebugStream(IAudioStream sourceStream, DebugStreamController debugController)
            : this(sourceStream)
        {
            debugController.Add(this);
        }

        public string Name
        {
            get { return "DS(" + sourceStream.GetType().Name + ")"; }
        }

        public override long Position
        {
            get { return base.Position; }
            set
            {
                calculatedLength += value - base.Position;
                base.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = base.Read(buffer, offset, count);
            totalBytesRead += bytesRead;
            calculatedLength += bytesRead;

            if (bytesRead == 0)
            {
                Debug.WriteLine(
                    "EOS {0,-40}: pos {1}, len {2}, clen {3}, tbr {4}",
                    Name,
                    Position,
                    Length,
                    calculatedLength,
                    totalBytesRead
                );
            }

            return bytesRead;
        }
    }
}
