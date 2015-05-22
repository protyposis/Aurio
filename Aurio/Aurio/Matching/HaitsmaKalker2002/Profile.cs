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

namespace Aurio.Matching.HaitsmaKalker2002 {
    /// <summary>
    /// Profile interface for the fingerprint generator.
    /// </summary>
    public abstract class Profile : IProfile {

        public string Name { get; protected set; }

        public int FrameSize { get; set; }

        public int FrameStep { get; set; }

        public int SampleRate { get; set; }

        public int MinFrequency { get; set; }

        public int MaxFrequency { get; set; }

        public int FrequencyBands { get; set; }

        public int FlipWeakestBits { get; set; }

        public double HashTimeScale { get; protected set; }

        /// <summary>
        /// Maps the frequency bins from the FFT result to the target frequency bins that the fingerprint
        /// will be generated from.
        /// </summary>
        /// <param name="inputBins">FFT result bins</param>
        /// <param name="outputBins">target bins</param>
        public abstract void MapFrequencies(float[] inputBins, float[] outputBins);
    }
}
