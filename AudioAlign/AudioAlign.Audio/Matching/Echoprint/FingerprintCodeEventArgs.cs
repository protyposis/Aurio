using AudioAlign.Audio.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    public class FingerprintCodeEventArgs : EventArgs {
        public AudioTrack AudioTrack { get; set; }
        public int Index { get; set; }
        public int Indices { get; set; }
        public List<FPCode> Codes { get; set; }
    }
}
