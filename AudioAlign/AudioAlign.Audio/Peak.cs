using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    public class Peak {
        private float min, max;

        public Peak(float min, float max) {
            this.min = min;
            this.max = max;
        }

        public float Min {
            get { return min; }
            set { min = value; }
        }

        public float Max {
            get { return max; }
            set { max = value; }
        }

        public override string ToString() {
            return "[" + min + ";" + max + "]";
        }
    }
}
