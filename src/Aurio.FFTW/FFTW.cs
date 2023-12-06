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
using System.Runtime.InteropServices;

namespace Aurio.FFTW
{
    public class FFTW : IDisposable
    {
        private static IInteropWrapper interop;

        static FFTW()
        {
            if (Environment.Is64BitProcess)
            {
                interop = new Interop64Wrapper();
            }
            else
            {
                throw new Exception("Unsupported platform");
            }
        }

        private int size;
        private float[] buffer;
        private GCHandle bufferHandle;
        private IntPtr plan;

        public FFTW(int size)
        {
            this.size = size;
            buffer = new float[2 * (size / 2 + 1)]; // http://fftw.org/fftw3_doc/One_002dDimensional-DFTs-of-Real-Data.html
            bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr pBuffer = bufferHandle.AddrOfPinnedObject();
            lock (interop)
            {
                plan = interop.dft_r2c_1d(size, pBuffer, pBuffer, fftw_flags.Estimate);
            }
        }

        public void Execute(float[] bufferInPlace)
        {
            Array.Copy(bufferInPlace, this.buffer, bufferInPlace.Length);
            interop.execute(plan);
            Array.Copy(this.buffer, bufferInPlace, bufferInPlace.Length);
        }

        public void Execute(float[] bufferIn, float[] bufferOut)
        {
            Array.Copy(bufferIn, this.buffer, bufferIn.Length);
            interop.execute(plan);
            Array.Copy(this.buffer, bufferOut, bufferOut.Length);
        }

        public void Dispose()
        {
            if (plan != IntPtr.Zero)
            {
                interop.destroy_plan(plan);
                bufferHandle.Free();
            }
        }

        ~FFTW()
        {
            Dispose();
        }
    }
}
