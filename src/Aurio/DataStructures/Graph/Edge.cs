//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

namespace Aurio.DataStructures.Graph
{
    public class Edge<TVertex, TWeight>
    {
        public Edge(TVertex v1, TVertex v2, TWeight weight)
        {
            Vertex1 = v1;
            Vertex2 = v2;
            Weight = weight;
        }

        public TVertex Vertex1 { get; private set; }

        public TVertex Vertex2 { get; private set; }

        public TWeight Weight { get; private set; }

        public object Tag { get; set; }
    }
}
