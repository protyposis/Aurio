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

namespace Aurio.Matching {
    public class MatchPair {

        public AudioTrack Track1 { get; set; }
        public AudioTrack Track2 { get; set; }
        public List<Match> Matches { get; set; }

        public double CalculateAverageSimilarity() {
            if (Matches == null || Matches.Count == 0) {
                return 0;
            }

            double similarity = 0;
            foreach (Match match in Matches) {
                similarity += match.Similarity;
            }
            return similarity /= Matches.Count;
        }

        public void SwapTracks() {
            AudioTrack temp = Track1;
            Track1 = Track2;
            Track2 = temp;
        }
    }
}
