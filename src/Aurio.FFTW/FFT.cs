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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.FFT;

namespace Aurio.FFTW
{
    public class FFT : IFFT
    {
        private readonly FFTW _instance;

        public FFT(int size)
        {
            Size = size;
            InPlace = true;

            _instance = new FFTW(size);
        }

        public int Size { get; private set; }

        public bool InPlace { get; private set; }

        public void Forward(float[] inPlaceBuffer)
        {
            _instance.Execute(inPlaceBuffer);
        }

        public void Forward(float[] input, float[] output)
        {
            _instance.Execute(input, output);
        }

        public void Backward(float[] inPlaceBuffer)
        {
            throw new NotImplementedException();
        }

        public void Backward(float[] input, float[] output)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _instance.Dispose();
        }

        ~FFT()
        {
            Dispose();
        }
    }
}
