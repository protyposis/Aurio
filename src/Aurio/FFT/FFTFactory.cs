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

namespace Aurio.FFT
{
    public static class FFTFactory
    {
        public static IFFTFactory Factory { get; set; }

        public static IFFT CreateInstance(int size)
        {
            if (Factory == null)
            {
                throw new NotSupportedException(
                    "Cannot create instance because no factory has been set."
                );
            }

            return Factory.CreateInstance(size);
        }
    }
}
