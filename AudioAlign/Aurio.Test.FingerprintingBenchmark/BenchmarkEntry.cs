using Aurio.Audio.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Test.FingerprintingBenchmark {
    class BenchmarkEntry {
        public AudioTrack Track { get; set; }
        public String Type { get; set; }
        public int HashCount { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
