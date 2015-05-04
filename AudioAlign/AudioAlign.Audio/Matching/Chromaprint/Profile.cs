using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    public class Profile {

        public Profile() {
            SamplingRate = 11025;
        }

        public int SamplingRate { get; set; }
    }
}
