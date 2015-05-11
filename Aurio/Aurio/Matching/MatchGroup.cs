using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Project;

namespace Aurio.Matching {
    public class MatchGroup {
        public TrackList<AudioTrack> TrackList { get; set; }
        public List<MatchPair> MatchPairs { get; set; }
    }
}
