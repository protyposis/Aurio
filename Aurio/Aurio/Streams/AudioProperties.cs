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

namespace Aurio.Streams {
    public class AudioProperties {

        public AudioProperties(int channels, int sampleRate, int bitDepth, AudioFormat format) {
            if (channels < 1) {
                throw new Exception("invalid number of channels: " + channels);
            }
            if (sampleRate < 1) {
                throw new Exception("invalid sample rate: " + sampleRate);
            }
            if (bitDepth < 1 || bitDepth % 8 != 0) {
                throw new Exception("invalid bit depth: " + bitDepth);
            }

            Channels = channels;
            SampleRate = sampleRate;
            BitDepth = bitDepth;
            Format = format;
        }

        public int Channels { get; internal set; }
        public int SampleRate { get; internal set; }
        public int BitDepth { get; internal set; }
        public AudioFormat Format { get; internal set; }

        public int SampleByteSize {
            get { return BitDepth / 8; }
        }

        public int SampleBlockByteSize {
            get { return SampleByteSize * Channels; }
        }

        public override string ToString() {
            return String.Format("{0}bit {1}Hz {2}ch {3}", BitDepth, SampleRate, Channels, Format);
        }
    }
}
