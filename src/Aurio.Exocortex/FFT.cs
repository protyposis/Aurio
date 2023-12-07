//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2018  Mario Guggenberger <mg@protyposis.net>
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
using Aurio.FFT;
using Exocortex.DSP;

namespace Aurio.Exocortex
{
    public class FFT : IFFT
    {
        public FFT(int size)
        {
            Size = size;
            InPlace = true;
        }

        public int Size { get; private set; }

        public bool InPlace { get; private set; }

        public void Forward(float[] inPlaceBuffer)
        {
            Fourier.RFFT(inPlaceBuffer, FourierDirection.Forward);
        }

        public void Forward(float[] input, float[] output)
        {
            // Copy input to output, then do an in-place transformation on the output. The result
            // will look like an out-of-place transformation.
            Array.Copy(input, output, input.Length);
            Fourier.RFFT(output, FourierDirection.Forward);
        }

        public void Backward(float[] inPlaceBuffer)
        {
            Fourier.RFFT(inPlaceBuffer, FourierDirection.Backward);
        }

        public void Backward(float[] input, float[] output)
        {
            // Copy input to output, then do an in-place transformation on the output. The result
            // will look like an out-of-place transformation.
            Array.Copy(input, output, input.Length);
            Fourier.RFFT(output, FourierDirection.Backward);
        }

        public void Dispose()
        {
            // Nothing to do
        }
    }
}
