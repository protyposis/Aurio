using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Matching.Chromaprint {
    class Quantizer {

        private double t0, t1, t2;

        public Quantizer(double t0, double t1, double t2) {
            this.t0 = t0;
            this.t1 = t1;
            this.t2 = t2;
        }

        public int Quantize(double value) {
            if (value < t1) {
                if (value < t0) {
                    return 0;
                }
                return 1;
            }
            else {
                if (value < t2) {
                    return 2;
                }
                return 3;
            }
        }
    }
}
