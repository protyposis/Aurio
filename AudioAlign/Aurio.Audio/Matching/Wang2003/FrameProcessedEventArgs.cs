using Aurio.Audio.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Matching.Wang2003 {
    public class FrameProcessedEventArgs : EventArgs {
        public AudioTrack AudioTrack { get; set; }
        public int Index { get; set; }
        public int Indices { get; set; }
        public float[] Spectrum { get; set; }
        public float[] SpectrumResidual { get; set; }
    }
}
