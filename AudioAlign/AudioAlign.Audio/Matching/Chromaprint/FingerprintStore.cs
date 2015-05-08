using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Diagnostics;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Matching.Chromaprint {
    public class FingerprintStore : HaitsmaKalker2002.FingerprintStore {

        public FingerprintStore(IProfile profile) : base(profile, "FP-CP") {
            //
        }
    }
}
