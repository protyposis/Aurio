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
    public class VolumeClipStream : AbstractAudioStreamWrapper
    {
        public VolumeClipStream(IAudioStream sourceStream)
            : base(sourceStream)
        {
            if (
                !(
                    sourceStream.Properties.Format == AudioFormat.IEEE
                    && sourceStream.Properties.BitDepth == 32
                )
            )
            {
                throw new ArgumentException(
                    "unsupported source format: " + sourceStream.Properties
                );
            }
            Clip = true;
        }

        /// <summary>
        /// Enables or disables audio sample clipping.
        /// </summary>
        public bool Clip { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = sourceStream.Read(buffer, offset, count);

            if (Clip && bytesRead > 0)
            {
                unsafe
                {
                    fixed (byte* sampleBuffer = &buffer[offset])
                    {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x++)
                        {
                            if (samples[x] > 1.0f)
                            {
                                samples[x] = 1.0f;
                            }
                            else if (samples[x] < -1.0f)
                            {
                                samples[x] = -1.0f;
                            }
                        }
                    }
                }
            }

            return bytesRead;
        }
    }
}
