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
using System.Runtime.InteropServices;

namespace Aurio.LibSampleRate {
    /// <summary>
    /// SRC_DATA is used to pass data to src_simple() and src_process().
    /// </summary>
    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal unsafe struct SRC_DATA {

        /// <summary>
        /// float*
        /// A pointer to the input data samples.
        /// </summary>
        public float* data_in;

        /// <summary>
        /// float*
        /// A pointer to the output data samples.
        /// </summary>
        public float* data_out;

        /// <summary>
        /// int
        /// The number of frames of data pointed to by data_in.
        /// </summary>
        public int input_frames;

        /// <summary>
        /// int
        /// Maximum number of frames pointer to by data_out.
        /// </summary>
        public int output_frames;

        /// <summary>
        /// int
        /// When the src_process function returns output_frames_gen will be set to the number of output frames 
        /// generated and input_frames_used will be set to the number of input frames consumed to generate the 
        /// provided number of output frames. 
        /// </summary>
        public int input_frames_used;

        /// <summary>
        /// int
        /// When the src_process function returns output_frames_gen will be set to the number of output frames 
        /// generated and input_frames_used will be set to the number of input frames consumed to generate the 
        /// provided number of output frames. 
        /// </summary>
        public int output_frames_gen;

        /// <summary>
        /// int
        /// Equal to 0 if more input data is available and 1 otherwise.
        /// </summary>
        public int end_of_input;

        /// <summary>
        /// double
        /// Equal to output_sample_rate / input_sample_rate.
        /// </summary>
        public double src_ratio;
    }
}
