using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AudioAlign.FFTW {
    public class FFTW {

        private static IInteropWrapper interop;

        static FFTW() {
            if (IntPtr.Size == 8) {
                interop = new Interop64Wrapper();
            }
            else {
                interop = new Interop32Wrapper();
            }
        }

        private int size;
        private float[] buffer;
        private GCHandle bufferHandle;
        private IntPtr plan;

        public FFTW(int size) {
            this.size = size;
            buffer = new float[2 * (size / 2 + 1)]; // http://fftw.org/fftw3_doc/One_002dDimensional-DFTs-of-Real-Data.html
            bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr pBuffer = bufferHandle.AddrOfPinnedObject();
            lock (interop) {
                plan = interop.dft_r2c_1d(size, pBuffer, pBuffer, fftw_flags.Estimate);
            }
        }

        ~FFTW() {
            interop.destroy_plan(plan);
            bufferHandle.Free();
        }

        public void Execute(float[] bufferInPlace) {
            Array.Copy(bufferInPlace, this.buffer, bufferInPlace.Length);
            interop.execute(plan);
            Array.Copy(this.buffer, bufferInPlace, bufferInPlace.Length);
        }

        public void Execute(float[] bufferIn, float[] bufferOut) {
            Array.Copy(bufferIn, this.buffer, bufferIn.Length);
            interop.execute(plan);
            Array.Copy(this.buffer, bufferOut, bufferOut.Length);
        }
    }
}
