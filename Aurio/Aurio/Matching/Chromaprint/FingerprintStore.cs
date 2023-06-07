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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Project;
using System.Diagnostics;
using Aurio.Streams;

namespace Aurio.Matching.Chromaprint
{
    public class FingerprintStore : HaitsmaKalker2002.FingerprintStore
    {
        public FingerprintStore(IProfile profile, IFingerprintCollisionMap collisionMap)
            : base(profile, collisionMap, "FP-CP")
        {
            //
        }

        public FingerprintStore(IProfile profile)
            : base(profile, new DictionaryCollisionMap(), "FP-CP")
        {
            //
        }
    }
}
