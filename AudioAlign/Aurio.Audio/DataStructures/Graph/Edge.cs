using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.DataStructures.Graph {
    public class Edge<TVertex, TWeight> {

        public Edge(TVertex v1, TVertex v2, TWeight weight) {
            Vertex1 = v1;
            Vertex2 = v2;
            Weight = weight;
        }

        public TVertex Vertex1 {
            get;
            private set;
        }

        public TVertex Vertex2 {
            get;
            private set;
        }

        public TWeight Weight {
            get;
            private set;
        }

        public object Tag { get; set; }
    }
}
