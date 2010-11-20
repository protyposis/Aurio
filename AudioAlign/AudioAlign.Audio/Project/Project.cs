using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Project {
    public class Project {

        private readonly TrackList<Track> trackList;

        public Project() {
            trackList = new TrackList<Track>();
        }

        public TrackList<Track> Tracks {
            get { return trackList; }
        }
    }
}
