using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Audio.Project;
using System.Diagnostics;
using Aurio.Audio.Streams;

namespace Aurio.Audio.Matching.Chromaprint {
    public class FingerprintStore : HaitsmaKalker2002.FingerprintStore {

        public FingerprintStore(IProfile profile) : base(profile, "FP-CP") {
            //
        }
    }
}
