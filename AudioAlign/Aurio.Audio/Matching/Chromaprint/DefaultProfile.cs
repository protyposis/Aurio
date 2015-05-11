using AudioAlign.Audio.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    class DefaultProfile : Profile {
        public DefaultProfile() {
            Name = "Chromaprint default";

            SamplingRate = 11025;
            WindowSize = 4096;
            HopSize = WindowSize / 3;
            WindowType = WindowType.Hamming;

            ChromaMinFrequency = 28;
            ChromaMaxFrequency = 3520;
            ChromaMappingMode = Chroma.MappingMode.Chromaprint;
            ChromaFilterCoefficients = new double[] { 0.25, 0.75, 1.0, 0.75, 0.25 }; // Gauss-filter(?) for temporal chroma smoothing
            ChromaNormalizationThreshold = 0.01;

            Classifiers = new Classifier[] {
                new Classifier(new Filter(0, 4, 15, 3), new Quantizer(1.98215, 2.35817, 2.63523)),
                new Classifier(new Filter(4, 4, 15, 6), new Quantizer(-1.03809, -0.651211, -0.282167)),
                new Classifier(new Filter(1, 0, 16, 4), new Quantizer(-0.298702, 0.119262, 0.558497)),
                new Classifier(new Filter(3, 8, 12, 2), new Quantizer(-0.105439, 0.0153946, 0.135898)),
                new Classifier(new Filter(3, 4, 8, 4), new Quantizer(-0.142891, 0.0258736, 0.200632)),
                new Classifier(new Filter(4, 0, 5, 3), new Quantizer(-0.826319, -0.590612, -0.368214)),
                new Classifier(new Filter(1, 2, 9, 2), new Quantizer(-0.557409, -0.233035, 0.0534525)),
                new Classifier(new Filter(2, 7, 4, 3), new Quantizer(-0.0646826, 0.00620476, 0.0784847)),
                new Classifier(new Filter(2, 6, 16, 2), new Quantizer(-0.192387, -0.029699, 0.215855)),
                new Classifier(new Filter(2, 1, 2, 3), new Quantizer(-0.0397818, -0.00568076, 0.0292026)),
                new Classifier(new Filter(5, 10, 15, 1), new Quantizer(-0.53823, -0.369934, -0.190235)),
                new Classifier(new Filter(3, 6, 10, 2), new Quantizer(-0.124877, 0.0296483, 0.139239)),
                new Classifier(new Filter(2, 1, 14, 1), new Quantizer(-0.101475, 0.0225617, 0.231971)),
                new Classifier(new Filter(3, 5, 4, 6), new Quantizer(-0.0799915, -0.00729616, 0.063262)),
                new Classifier(new Filter(1, 9, 12, 2), new Quantizer(-0.272556, 0.019424, 0.302559)),
                new Classifier(new Filter(3, 4, 14, 2), new Quantizer(-0.164292, -0.0321188, 0.0846339))
            };

            HashTimeScale = 1d / SamplingRate * HopSize;
        }
    }
}
