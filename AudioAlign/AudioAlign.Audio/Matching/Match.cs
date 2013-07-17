using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;

namespace AudioAlign.Audio.Matching {
    public class Match {
        public AudioTrack Track1 { get; set; }
        public AudioTrack Track2 { get; set; }
        public TimeSpan Track1Time { get; set; }
        public TimeSpan Track2Time { get; set; }
        public float Similarity { get; set; }
        public string Source { get; set; }

        public Match() {}

        public Match(Match m) {
            Track1 = m.Track1;
            Track2 = m.Track2;
            Track1Time = m.Track1Time;
            Track2Time = m.Track2Time;
            Similarity = m.Similarity;
            Source = m.Source;
        }

        public TimeSpan Offset {
            get {
                return Track1Time - Track2Time;
            }
        }

        public override string ToString() {
            return "Match {" + Track1.Name + "@" + Track1Time + "<->" + Track2.Name + "@" + Track2Time + ":" + Similarity + " (" + Source + ")}";
        }

        public void SwapTracks() {
            AudioTrack tempTrack = Track1;
            Track1 = Track2;
            Track2 = tempTrack;

            TimeSpan tempTime = Track1Time;
            Track1Time = Track2Time;
            Track2Time = tempTime;
        }
    }
}
