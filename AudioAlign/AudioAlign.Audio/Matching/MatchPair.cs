using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;

namespace AudioAlign.Audio.Matching {
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
    }
}
