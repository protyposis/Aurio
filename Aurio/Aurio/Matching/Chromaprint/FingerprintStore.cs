using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Project;
using System.Diagnostics;
using Aurio.Streams;

namespace Aurio.Matching.Chromaprint {
    public class FingerprintStore : HaitsmaKalker2002.FingerprintStore {

        public FingerprintStore(IProfile profile) : base(profile, "FP-CP") {
            //
        }
    }
}
