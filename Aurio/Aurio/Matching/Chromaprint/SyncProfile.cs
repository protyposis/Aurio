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

using Aurio.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Chromaprint {
    class SyncProfile : DefaultProfile {
        public SyncProfile() : base() {
            Name = "Sync optimized";

            // The same settings as in the Haitsma&Kalker default profile
            SamplingRate = 5512;
            WindowSize = 2048;
            HopSize = 64;

            ChromaMappingMode = Chroma.MappingMode.Paper;

            HashTimeScale = 1d / SamplingRate * HopSize;
        }
    }
}
