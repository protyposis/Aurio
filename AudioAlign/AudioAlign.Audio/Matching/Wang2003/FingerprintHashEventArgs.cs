using AudioAlign.Audio.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    public class FingerprintHashEventArgs : EventArgs {
        public AudioTrack AudioTrack { get; set; }
        public int Index { get; set; }
        public int Indices { get; set; }
        public List<uint> Hashes { get; set; }
    }
}
