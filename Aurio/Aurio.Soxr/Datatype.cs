// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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

namespace Aurio.Soxr {
    enum Datatype : uint {
        SOXR_FLOAT32_I = 0,
        SOXR_FLOAT64_I,
        SOXR_INT32_I,
        SOXR_INT16_I,

        SOXR_FLOAT32_S = 4,
        SOXR_FLOAT64_S,
        SOXR_INT32_S,
        SOXR_INT16_S
    }
}
