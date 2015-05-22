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
using System.Text;
using NAudio.Wave;

namespace Aurio.Streams {
    public class NAudioSourceStream : IAudioStream {

        private WaveStream sourceStream;
        private AudioProperties properties;

        public NAudioSourceStream(WaveStream sourceStream) {
            WaveFormat sourceFormat = sourceStream.WaveFormat;
            AudioFormat format;
            // check for supported formats:
            if(sourceFormat.Encoding == WaveFormatEncoding.Pcm && sourceFormat.BitsPerSample == 16) {
                format = AudioFormat.LPCM;
            }
            else if (sourceFormat.Encoding == WaveFormatEncoding.Pcm && sourceFormat.BitsPerSample == 24) {
                format = AudioFormat.LPCM;
            }
            else if(sourceFormat.Encoding == WaveFormatEncoding.IeeeFloat && sourceFormat.BitsPerSample == 32) {
                format = AudioFormat.IEEE;
            }
            else {
                throw new ArgumentException(String.Format("unsupported source format: {0}bit {1}Hz {2}ch {3}",
                    sourceFormat.BitsPerSample, sourceFormat.SampleRate, sourceFormat.Channels, sourceFormat.Encoding));
            }

            this.sourceStream = sourceStream;
            this.properties = new AudioProperties(sourceFormat.Channels, sourceFormat.SampleRate, sourceFormat.BitsPerSample, format);
        }

        public AudioProperties Properties {
            get { return properties; }
        }

        public long Length {
            get { return sourceStream.Length; }
        }

        public long Position {
            get { 
                return sourceStream.Position; 
            }
            set {
                if (value % SampleBlockSize == 0) {
                    sourceStream.Position = value;
                }
                else {
                    throw new Exception("position must be aligned to the sample block size");
                }
            }
        }

        public int SampleBlockSize {
            get { return sourceStream.BlockAlign; }
        }

        public int Read(byte[] buffer, int offset, int count) {
            if (count % SampleBlockSize != 0) {
                // an unaligned read length would lead to an illegal unaligned position in the stream
                throw new Exception("read length must be a multiple of the sample block size");
            }

            int bytesRead = sourceStream.Read(buffer, offset, count);

            if (bytesRead % SampleBlockSize != 0) {
                // an unaligned number of read bytes leads to an illegal unaligned position in the stream
                throw new Exception("the number of read bytes is not a multiple of the sample block size");
            }

            return bytesRead;
        }
    }
}
