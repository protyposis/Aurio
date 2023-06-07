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

namespace Aurio.FFmpeg
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VideoOutputFormat
    {
        public int width { get; internal set; }
        public int height { get; internal set; }
        public double frame_rate { get; internal set; }
        public double aspect_ratio { get; internal set; }
    }

    public enum AVPictureType : int
    {
        AV_PICTURE_TYPE_NONE = 0,
        AV_PICTURE_TYPE_I,
        AV_PICTURE_TYPE_P,
        AV_PICTURE_TYPE_B,
        AV_PICTURE_TYPE_S,
        AV_PICTURE_TYPE_SI,
        AV_PICTURE_TYPE_SP,
        AV_PICTURE_TYPE_BI
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VideoFrameProperties
    {
        public bool keyframe { get; internal set; }
        public AVPictureType pict_type { get; internal set; }
        public bool interlaced { get; internal set; }
        public bool top_field_first { get; internal set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VideoOutputConfig
    {
        public VideoOutputFormat format { get; internal set; }
        public long length { get; internal set; }
        public int frame_size { get; internal set; }
        public VideoFrameProperties current_frame { get; internal set; }
    }
}
