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
using System.Runtime.InteropServices;
using System.Text;

namespace Aurio.FFmpeg {

    [StructLayout(LayoutKind.Sequential)]
    public struct AudioOutputFormat {
        public int sample_rate { get; internal set; }
        public int sample_size { get; internal set; }
        public int channels { get; internal set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AudioOutputConfig {
        public AudioOutputFormat format { get; internal set; }
        public long length { get; internal set; }
        public int frame_size { get; internal set; }
    }
}
