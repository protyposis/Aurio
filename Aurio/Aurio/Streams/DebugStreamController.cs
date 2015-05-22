// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Aurio.Streams {
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
