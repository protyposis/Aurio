﻿//
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

using Aurio.Resampler;

namespace Aurio.Soxr
{
    public class ResamplerFactory : IResamplerFactory
    {
        public ResamplerFactory()
        {
            SoxResampler.ValidateNativeLibraryAvailability();
        }

        public IResampler CreateInstance(
            ResamplingQuality quality,
            int channels,
            double sampleRateRatio
        )
        {
            return new Resampler(quality, channels, sampleRateRatio);
        }

        public bool CheckRatio(double sampleRateRatio)
        {
            return SoxResampler.CheckRatio(sampleRateRatio);
        }
    }
}
