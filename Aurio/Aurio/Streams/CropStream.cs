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

namespace Aurio.Streams {
    public class CropStream : AbstractAudioStreamWrapper {

        private long begin, end;

        public CropStream(IAudioStream sourceStream)
            : base(sourceStream) {
            this.begin = 0;
            this.end = sourceStream.Length;
        }

        public CropStream(IAudioStream sourceStream, long begin, long end)
            : this(sourceStream) {
            ValidateCropBounds(begin, end);
            Begin = begin;
            End = end;
        }

        private void ValidateCropBounds(long begin, long end) {
            if (begin < 0 || begin > sourceStream.Length) {
                throw new ArgumentOutOfRangeException("begin");
            }
            if (end < 0 || end > sourceStream.Length) {
                throw new ArgumentOutOfRangeException("end");
            }
            if (begin > end) {
                throw new ArgumentOutOfRangeException("begin after end");
            }
        }

        public long Begin {
            get { return begin; }
            set { 
                ValidateCropBounds(value, end); 
                ValidateSampleBlockAlignment(value);
                begin = value;
                if (begin > base.Position) {
                    Position = 0;
                }
            }
        }

        public long End {
            get { return end; }
            set { 
                ValidateCropBounds(begin, value); 
                ValidateSampleBlockAlignment(value); 
                end = value;
                if (end < base.Position) {
                    Position = Length;
                }
            }
        }

        public override long Position {
            get { return base.Position - begin; }
            set { base.Position = value + begin; }
        }

        public override long Length {
            get { return end - begin; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return base.Read(buffer, offset, Length - Position < count ? (int)(Length - Position) : count);
        }
    }
}
