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

using Aurio.FFT;

namespace Aurio.PFFFT
{
    public class FFT : IFFT
    {
        private readonly PFFFT _instance;

        public FFT(int size)
        {
            Size = size;
            InPlace = true;

            _instance = new PFFFT(size, Transform.Real);
        }

        public int Size { get; private set; }

        public bool InPlace { get; private set; }

        public void Forward(float[] inPlaceBuffer)
        {
            _instance.Forward(inPlaceBuffer);
        }

        public void Forward(float[] input, float[] output)
        {
            _instance.Forward(input, output);
        }

        public void Backward(float[] inPlaceBuffer)
        {
            _instance.Backward(inPlaceBuffer);
        }

        public void Backward(float[] input, float[] output)
        {
            _instance.Backward(input, output);
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
