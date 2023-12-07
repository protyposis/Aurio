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
using Aurio.Project;

namespace Aurio.Matching
{
    public class Match
    {
        public AudioTrack Track1 { get; set; }
        public AudioTrack Track2 { get; set; }
        public TimeSpan Track1Time { get; set; }
        public TimeSpan Track2Time { get; set; }
        public float Similarity { get; set; }
        public string Source { get; set; }

        public Match() { }

        public Match(Match m)
        {
            Track1 = m.Track1;
            Track2 = m.Track2;
            Track1Time = m.Track1Time;
            Track2Time = m.Track2Time;
            Similarity = m.Similarity;
            Source = m.Source;
        }

        public TimeSpan Offset
        {
            get { return (Track1.Offset + Track1Time) - (Track2.Offset + Track2Time); }
        }

        public override string ToString()
        {
            return "Match {"
                + Track1.Name
                + "@"
                + Track1Time
                + "<->"
                + Track2.Name
                + "@"
                + Track2Time
                + ":"
                + Similarity
                + " ("
                + Source
                + ")}";
        }

        public void SwapTracks()
        {
            AudioTrack tempTrack = Track1;
            Track1 = Track2;
            Track2 = tempTrack;

            TimeSpan tempTime = Track1Time;
            Track1Time = Track2Time;
            Track2Time = tempTime;
        }
    }
}
