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

using Aurio.Features;

namespace Aurio.Matching.Chromaprint
{
    public abstract class Profile : IProfile
    {
        /// <summary>
        /// The name of this profile.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// The sampling rate at which the STFT is taken.
        /// </summary>
        public int SamplingRate { get; set; }

        /// <summary>
        /// The STFT window size in samples, which is the double of the spectral frame resolution.
        /// </summary>
        public int WindowSize { get; set; }

        /// <summary>
        /// The distance in samples from one STFT window to the next. Can be overlapping.
        /// </summary>
        public int HopSize { get; set; }

        /// <summary>
        /// The type of window used for the STFT.
        /// </summary>
        public WindowType WindowType { get; set; }

        /// <summary>
        /// The lower bound of the frequency range from which the chroma is extracted.
        /// </summary>
        public float ChromaMinFrequency { get; set; }

        /// <summary>
        /// The upper bound of the frequency range from which the chroma is extracted.
        /// </summary>
        public float ChromaMaxFrequency { get; set; }

        /// <summary>
        /// The mode in which spectral bins are mapped to chroma bins.
        /// </summary>
        public Chroma.MappingMode ChromaMappingMode { get; set; }

        /// <summary>
        /// The FIR filter coefficients used to filter/smooth the chroma bins over time.
        /// </summary>
        public double[] ChromaFilterCoefficients { get; set; }

        /// <summary>
        /// The threshold for the euclidean norm of a chroma feature vector, below which
        /// all chroma bis are set to zero.
        /// </summary>
        public double ChromaNormalizationThreshold { get; set; }

        /// <summary>
        /// The classifiers used to convert the chromatrogram to hashes.
        /// </summary>
        internal Classifier[] Classifiers { get; set; }

        public double HashTimeScale { get; protected set; }
    }
}
