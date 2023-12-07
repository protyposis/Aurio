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

namespace Aurio.Soxr
{
    public enum QualityRecipe : uint
    {
        /// <summary>
        /// 'Quick' cubic interpolation
        /// </summary>
        SOXR_QQ = 0,

        /// <summary>
        /// 'Low' 16-bit with larger rolloff
        /// </summary>
        SOXR_LQ = 1,

        /// <summary>
        /// 'Medium' 16-bit with medium rolloff.
        /// </summary>
        SOXR_MQ = 2,

        /// <summary>
        /// 'High quality'
        /// </summary>
        SOXR_HQ = SOXR_20_BITQ,

        /// <summary>
        /// 'Very high quality'
        /// </summary>
        SOXR_VHQ = SOXR_28_BITQ,

        SOXR_16_BITQ = 3,
        SOXR_20_BITQ = 4,
        SOXR_24_BITQ = 5,
        SOXR_28_BITQ = 6,
        SOXR_32_BITQ = 7,

        /// <summary>
        /// Libsamplerate equivalent 'Best sinc'
        /// </summary>
        SOXR_LSR0Q = 8,

        /// <summary>
        /// Libsamplerate equivalent 'Medium sinc'
        /// </summary>
        SOXR_LSR1Q = 9,

        /// <summary>
        /// Libsamplerate equivalent 'Fast sinc'
        /// </summary>
        SOXR_LSR2Q = 10,

        SOXR_LINEAR_PHASE = 0x00,
        SOXR_INTERMEDIATE_PHASE = 0x10,
        SOXR_MINIMUM_PHASE = 0x30,
        SOXR_STEEP_FILTER = 0x40
    }
}
