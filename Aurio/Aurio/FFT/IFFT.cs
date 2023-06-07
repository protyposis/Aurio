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

namespace Aurio.FFT
{
    public interface IFFT : IDisposable
    {
        /// <summary>
        /// The FFT size that this instance expects.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Tells if this FFT instance supports in-place transformation. If it does not,
        /// the in-place methods can still be used, but an intermediate buffer with data copies
        /// will be used to emulate the behavior (with negative performance implications).
        /// </summary>
        bool InPlace { get; }

        /// <summary>
        /// Transforms samples from the time to the frequency domain.
        /// </summary>
        void Forward(float[] inPlaceBuffer);

        /// <summary>
        /// Transforms samples from the time to the frequency domain.
        /// </summary>
        void Forward(float[] input, float[] output);

        /// <summary>
        /// Transforms samples from the frequency to the time domain.
        /// </summary>
        void Backward(float[] inPlaceBuffer);

        /// <summary>
        /// Transforms samples from the frequency to the time domain.
        /// </summary>
        void Backward(float[] input, float[] output);
    }
}
