using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Graph {
    public class UndirectedGraph<TVertex, TWeight> where TWeight : IComparable<TWeight> {

        private List<TVertex> vertices;
        private List<Edge<TVertex, TWeight>> edges;

        public UndirectedGraph() {
            vertices = new List<TVertex>();
            edges = new List<Edge<TVertex, TWeight>>();
        }

        public void Add(Edge<TVertex, TWeight> e) {
            edges.Add(e);
            if (!vertices.Contains(e.Vertex1)) {
                vertices.Add(e.Vertex1);
            }
            if (!vertices.Contains(e.Vertex2)) {
                vertices.Add(e.Vertex2);
            }
        }

        public List<TVertex> Vertices {
            get { return new List<TVertex>(vertices); }
        }

        public List<Edge<TVertex, TWeight>> Edges {
            get { return new List<Edge<TVertex, TWeight>>(edges); }
        }

        public List<Edge<TVertex, TWeight>> GetEdges(TVertex v) {
            List<Edge<TVertex, TWeight>> edgesForVertex = new List<Edge<TVertex, TWeight>>();
            foreach (Edge<TVertex, TWeight> e in edges) {
                if (e.Vertex1.Equals(v) || e.Vertex2.Equals(v)) {
                    edgesForVertex.Add(e);
                }
            }
            return edgesForVertex;
        }

        /// <summary>
        /// Returns true if there's a path between the given vertices, else false.
        /// </summary>
        public bool IsConnected(TVertex v1, TVertex v2) {
            if (!vertices.Contains(v1) || !vertices.Contains(v2)) {
                throw new Exception("the given vertices aren't part of the graph");
            }

            int n = vertices.Count;

            // build adjacency matrix
            bool[,] m = new bool[n, n];
            foreach (Edge<TVertex, TWeight> e in edges) {
                int i1 = vertices.IndexOf(e.Vertex1);
                int i2 = vertices.IndexOf(e.Vertex2);
                m[i1, i2] = true;
                m[i2, i1] = true;
            }

            // Floyd–Warshall algorithm
            // http://stackoverflow.com/questions/684302/how-can-i-search-a-graph-for-a-path
            // http://en.wikipedia.org/wiki/Floyd%E2%80%93Warshall_algorithm
            for(int k = 0; k < n; k++) {
                for(int i = 0; i < n; i++) {
                    for(int j = 0; j < n; j++) {
                        m[i,j] = m[i,j] || (m[i,k] && m[k, j]);
                    }
                }
            }

            return m[vertices.IndexOf(v1), vertices.IndexOf(v2)];
        }

        /// <summary>
        /// Return true if the graph consists of disconnected components, else false.
        /// </summary>
        public bool IsDisconnected {
            get {
                for (int i = 0; i < vertices.Count; i++) {
                    for (int j = i + 1; j < vertices.Count; j++) {
                        if (!IsConnected(vertices[i], vertices[j])) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Traverses the graph starting at the given vertex and adds all visited edges to the list.
        /// </summary>
        /// <param name="edges">the list that will contain all traversed edges that are connected to the starting vertex</param>
        /// <param name="start">the vertex from which the graph should be traversed</param>
        private void GetConnectedEdges(List<Edge<TVertex, TWeight>> edges, TVertex start) {
            foreach (Edge<TVertex, TWeight> e in GetEdges(start)) {
                if (!edges.Contains(e)) {
                    edges.Add(e);
                    GetConnectedEdges(edges, e.Vertex1.Equals(start) ? e.Vertex2 : e.Vertex1);
                }
            }
        }

        /// <summary>
        /// Returns the connected component that contains the given vertex as a new graph.
        /// </summary>
        public UndirectedGraph<TVertex, TWeight> GetConnectedComponent(TVertex start) {
            List<Edge<TVertex, TWeight>> connectedEdges = new List<Edge<TVertex, TWeight>>();

            GetConnectedEdges(connectedEdges, start);

            UndirectedGraph<TVertex, TWeight> component = new UndirectedGraph<TVertex, TWeight>();
            foreach (Edge<TVertex, TWeight> e in connectedEdges) {
                component.Add(e);
            }

            return component;
        }

        /// <summary>
        /// Returns all connected components of the graph as a list of connected graphs.
        /// </summary>
        /// <returns></returns>
        public List<UndirectedGraph<TVertex, TWeight>> GetConnectedComponents() {
            List<UndirectedGraph<TVertex, TWeight>> connectedComponents = new List<UndirectedGraph<TVertex, TWeight>>();
            List<TVertex> visitedVertices = new List<TVertex>();

            foreach (TVertex v in vertices) {
                if (!visitedVertices.Contains(v)) {
                    UndirectedGraph<TVertex, TWeight> cc = GetConnectedComponent(v);
                    visitedVertices.AddRange(cc.Vertices);
                    connectedComponents.Add(cc);
                }
            }

            return connectedComponents;
        }

        public UndirectedGraph<TVertex, TWeight> GetMinimalSpanningTree() {
            if (IsDisconnected) {
                throw new Exception("cannot determine the MST in a graph that isn't fully connected");
            }

            // http://msdn.microsoft.com/en-us/library/ms379574.aspx
            // http://en.wikipedia.org/wiki/Prim%27s_algorithm
            List<TVertex> mstVertices = new List<TVertex>();
            List<Edge<TVertex, TWeight>> mstEdges = new List<Edge<TVertex, TWeight>>();

            mstVertices.Add(vertices[0]);
            while (mstVertices.Count < vertices.Count) {
                int index = 0;
                Edge<TVertex, TWeight> minEdge = null;
                foreach (Edge<TVertex, TWeight> e in edges) {
                    if ((mstVertices.Contains(e.Vertex1) && !mstVertices.Contains(e.Vertex2))
                        || mstVertices.Contains(e.Vertex2) && !mstVertices.Contains(e.Vertex1)) {
                            if (minEdge == null) {
                            minEdge = e;
                        }
                        else {
                            if (e.Weight.CompareTo(minEdge.Weight) < 0) {
                                minEdge = e;
                            }
                        }
                    }
                    index++;
                }
                if (minEdge == null) {
                    break;
                }
                mstVertices.Add(mstVertices.Contains(minEdge.Vertex1) ? minEdge.Vertex2 : minEdge.Vertex1);
                mstEdges.Add(minEdge);
            }

            UndirectedGraph<TVertex, TWeight> mstGraph = new UndirectedGraph<TVertex, TWeight>();
            foreach (Edge<TVertex, TWeight> e in mstEdges) {
                mstGraph.Add(e);
            }

            return mstGraph;
        }
    }
}
