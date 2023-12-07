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

using Aurio.Project;

namespace Aurio.Matching
{
    /// <summary>
    /// Struct consumes a little bit less memory than object (~5%) and results in a faster fingerprint
    /// generation time (~9%).
    ///
    ///
    /// </summary>
    public struct SubFingerprintLookupEntry
    {
        public SubFingerprintLookupEntry(AudioTrack audioTrack, int index)
        {
            AudioTrack = audioTrack;
            Index = index;
        }

        // NOTE fields instead of properties to speed up fingerprint search (getters took ~20% cpu time)
        public AudioTrack AudioTrack;
        public int Index;

        public override string ToString()
        {
            return "SubFingerprintLookupEntry {" + AudioTrack.GetHashCode() + " / " + Index + "}";
        }
    }
}
