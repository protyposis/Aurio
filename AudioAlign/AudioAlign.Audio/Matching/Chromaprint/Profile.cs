using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    public abstract class Profile {

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
        public int WindowSize {  get; set;}

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
        public ChromaMappingMode ChromaMappingMode { get; set; }

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
        /// The classifiers used to convert the chromatrogram to subfingerprints.
        /// </summary>
        internal Classifier[] Classifiers { get; set; }
    }
}
