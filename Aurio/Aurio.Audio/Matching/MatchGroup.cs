using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Audio.Project;

namespace Aurio.Audio.Matching {
    public class MatchGroup {
        public TrackList<AudioTrack> TrackList { get; set; }
        public List<MatchPair> MatchPairs { get; set; }
    }
}
