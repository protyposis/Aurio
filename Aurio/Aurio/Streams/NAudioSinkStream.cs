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
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace Aurio.Streams {
    public class NAudioSinkStream : WaveStream {

        private IAudioStream sourceStream;

        private WaveFormat waveFormat;

        public NAudioSinkStream(IAudioStream sourceStream) {
            AudioProperties sourceProperties = sourceStream.Properties;

            if (sourceProperties.Format == AudioFormat.LPCM) {
                waveFormat = WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.Pcm, 
                    sourceProperties.SampleRate,
                    sourceProperties.Channels,
                    sourceProperties.SampleRate * sourceProperties.Channels * sourceProperties.SampleByteSize,
                    sourceProperties.Channels * sourceProperties.SampleByteSize, sourceProperties.BitDepth);
            }
            else if (sourceProperties.Format == AudioFormat.IEEE) {
                waveFormat = WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.IeeeFloat,
                    sourceProperties.SampleRate,
                    sourceProperties.Channels,
                    sourceProperties.SampleRate * sourceProperties.Channels * sourceProperties.SampleByteSize,
                    sourceProperties.Channels * sourceProperties.SampleByteSize, sourceProperties.BitDepth);
            }
            else {
                throw new ArgumentException("unsupported source format: " + sourceProperties.ToString());
            }

            this.sourceStream = sourceStream;
        }

        public override WaveFormat WaveFormat {
            get { return waveFormat; }
        }

        public override long Length {
            get { return sourceStream.Length; }
        }

        public override long Position {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (count % BlockAlign != 0) {
                throw new Exception("misaligned read length!");
            }
            if(count == 0) {
                return 0;
            }
            return sourceStream.Read(buffer, offset, count);
        }
    }
}
