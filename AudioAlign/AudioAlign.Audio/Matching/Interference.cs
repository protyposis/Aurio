using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching {
    public class Interference {
        public unsafe static double DestructiveInverted(byte[] x, byte[] y) {
            if (x.Length != y.Length) {
                throw new ArgumentException("interval lengths do not match");
            }
            fixed (byte* xB = &x[0], yB = &y[0]) {
                float* xF = (float*)xB;
                float* yF = (float*)yB;
                int n = x.Length / sizeof(float);

                /* Calculate the mean of the two series x[], y[] */
                double mx = 0;
                double my = 0;
                for (int i = 0; i < n; i++) {
                    mx += xF[i];
                    my += yF[i];
                }
                mx /= n;
                my /= n;

                double diff;
                double result = 0;
                for (int i = 0; i < n; i++) {
                    // remove an eventually existing offset by subtracting the mean
                    diff = ((xF[i] - mx) - (yF[i] - my));
                    result += 1 - (diff > 0 ? diff : -diff);
                }

                return result / n;
            }
        }
    }
}
