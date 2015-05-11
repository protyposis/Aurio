using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.FFTW {
    internal interface IInteropWrapper {
        IntPtr dft_r2c_1d(int n, IntPtr input, IntPtr output, fftw_flags flags);
        void execute(IntPtr plan);
        void destroy_plan(IntPtr plan);
    }
}
