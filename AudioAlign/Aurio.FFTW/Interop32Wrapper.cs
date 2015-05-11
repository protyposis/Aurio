using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.FFTW {
    internal class Interop32Wrapper :IInteropWrapper {
        #region IInteropWrapper Members

        public IntPtr dft_r2c_1d(int n, IntPtr input, IntPtr output, fftw_flags flags) {
            return fftwf32.dft_r2c_1d(n, input, output, flags);
        }

        public void execute(IntPtr plan) {
            fftwf32.execute(plan);
        }

        public void destroy_plan(IntPtr plan) {
            fftwf32.destroy_plan(plan);
        }

        #endregion
    }
}
