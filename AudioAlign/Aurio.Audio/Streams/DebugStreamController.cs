using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class DebugStreamController {

        private List<DebugStream> streams;

        public DebugStreamController() {
            streams = new List<DebugStream>();
        }

        public void Add(DebugStream stream) {
            streams.Add(stream);
        }

        public bool Remove(DebugStream stream) {
            return streams.Remove(stream);
        }

        public void PrintPositions() {
            PrintPositions("--------------------");
        }

        public void PrintPositions(string tag) {
            Debug.WriteLine("--------- {0,-20} ---------", (object)tag);
            foreach (DebugStream stream in streams) {
                Debug.WriteLine("{0,-40} pos {1}", stream.Name, stream.Position);
            }
            Debug.WriteLine("----------------------------------------");
        }
    }
}
