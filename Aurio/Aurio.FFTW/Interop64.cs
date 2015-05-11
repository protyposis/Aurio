using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Aurio.FFTW {
    // taken from: http://www.sdss.jhu.edu/~tamas/bytes/fftwcsharp.html (no license applied)

    // FFTW Interop Classes
    #region Single Precision
    /// <summary>
    /// Contains the Basic Interface FFTW functions for single-precision (float) operations
    /// </summary>
    public class fftwf64 {

        private const string FFTWLIB = "libfftw3f-3.x64.dll";
        private const CallingConvention CALLINGCONVENTION = CallingConvention.Cdecl;

        /// <summary>
        /// Deallocates an FFTW plan and all associated resources
        /// </summary>
        /// <param name="plan">Pointer to the plan to release</param>
        [DllImport(FFTWLIB,
             EntryPoint = "fftwf_destroy_plan",
             ExactSpelling = true,
             CallingConvention = CALLINGCONVENTION)]
        public static extern void destroy_plan(IntPtr plan);

        /// <summary>
        /// Executes an FFTW plan, provided that the input and output arrays still exist
        /// </summary>
        /// <param name="plan">Pointer to the plan to execute</param>
        /// <remarks>execute (and equivalents) is the only function in FFTW guaranteed to be thread-safe.</remarks>
        [DllImport(FFTWLIB,
             EntryPoint = "fftwf_execute",
             ExactSpelling = true,
             CallingConvention = CALLINGCONVENTION)]
        public static extern void execute(IntPtr plan);

        /// <summary>
        /// Creates a plan for a 1-dimensional real-to-complex DFT
        /// </summary>
        /// <param name="n">Number of REAL (input) elements in the transform</param>
        /// <param name="input">Pointer to an array of 4-byte real numbers</param>
        /// <param name="output">Pointer to an array of 8-byte complex numbers</param>
        /// <param name="flags">Flags that specify the behavior of the planner</param>
        [DllImport(FFTWLIB,
             EntryPoint = "fftwf_plan_dft_r2c_1d",
             ExactSpelling = true,
             CallingConvention = CALLINGCONVENTION)]
        public static extern IntPtr dft_r2c_1d(int n, IntPtr input, IntPtr output, fftw_flags flags);

    }
    #endregion
}
