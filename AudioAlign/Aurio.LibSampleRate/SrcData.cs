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
