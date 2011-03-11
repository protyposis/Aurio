using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AudioAlign.Audio {
    [StructLayout(LayoutKind.Sequential)]
    public struct Peak {
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

        public void Merge(Peak p) {
            Merge(p.min, p.max);
        }

        public void Merge(float min, float max) {
            if (this.min > min) {
                this.min = min;
            }
            if (this.max < max) {
                this.max = max;
            }
        }

        public override string ToString() {
            return "Peak [" + min + ";" + max + "]";
        }
    }
}
