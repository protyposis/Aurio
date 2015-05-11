using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Chromaprint {
    class Classifier {

        private Filter filter;
        private Quantizer quantizer;

        public Classifier(Filter filter, Quantizer quantizer) {
            this.filter = filter;
            this.quantizer = quantizer;
        }

        public Filter Filter {
            get { return filter; }
        }

        public Quantizer Quantizer {
            get { return quantizer; }
        }

        public int Classify(IntegralImage image, int offset) {
            var filterValue = filter.Apply(image, offset);
            return quantizer.Quantize(filterValue);
        }
    }
}
