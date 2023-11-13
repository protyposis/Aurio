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
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Aurio.Matching
{
    [DebuggerDisplay("{Index}/{Hash}")]
    public struct SubFingerprint
    {
        public int Index;
        public SubFingerprintHash Hash;

        /// <summary>
        /// Gets a value telling if the subfingerprint is a variation of an original subfingerprint (which means
        /// the original subfingerprint has been modified) or an unmodified original subfingerprint.
        /// </summary>
        public bool IsVariation;

        public SubFingerprint(int index, SubFingerprintHash hash, bool variation)
        {
            Index = index;
            Hash = hash;
            IsVariation = variation;
        }
    }
}
