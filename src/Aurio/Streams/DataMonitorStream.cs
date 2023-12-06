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

namespace Aurio.Streams
{
    public class DataMonitorStream : AbstractAudioStreamWrapper
    {
        public event EventHandler<StreamDataMonitorEventArgs> DataRead;

        public DataMonitorStream(IAudioStream sourceStream)
            : base(sourceStream)
        {
            if (
                sourceStream.Properties.BitDepth != 32
                && sourceStream.Properties.Format != AudioFormat.IEEE
            )
            {
                throw new ArgumentException(
                    "Metering Stream expects 32 bit floating point audio",
                    "sourceStream"
                );
            }
        }

        public bool Disabled { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Disabled)
            {
                return sourceStream.Read(buffer, offset, count);
            }

            int bytesRead = sourceStream.Read(buffer, offset, count);
            if (DataRead != null && bytesRead > 0)
            {
                DataRead(
                    this,
                    new StreamDataMonitorEventArgs(Properties, buffer, offset, bytesRead)
                );
            }
            return bytesRead;
        }
    }

    public class StreamDataMonitorEventArgs : EventArgs
    {
        public StreamDataMonitorEventArgs(
            AudioProperties properties,
            byte[] buffer,
            int offset,
            int length
        )
        {
            Properties = properties;
            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        public byte[] Buffer { get; private set; }

        public int Offset { get; private set; }

        public int Length { get; private set; }

        public AudioProperties Properties { get; private set; }
    }
}
