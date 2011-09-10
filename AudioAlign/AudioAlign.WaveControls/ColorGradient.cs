using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace AudioAlign.WaveControls {
    public class ColorGradient {

        public class Stop {

            private Color color;
            private float offset;

            public Stop(Color color, float offset) {
                this.color = color;
                this.offset = offset;
            }

            public Color Color {
                get { return color; }
            }

            public float Offset {
                get { return offset; }
            }

            public override string ToString() {
                return "GradientStop{color=" + color + ";offset=" + offset + "}";
            }
        }

        private float start;
        private float end;
        private List<Stop> stops;

        public ColorGradient(float start, float end) {
            if (start > end) {
                throw new ArgumentException("start must be <= end");
            }
            this.start = start;
            this.end = end;
            this.stops = new List<Stop>();
        }

        public float Start {
            get { return start; }
        }

        public float End {
            get { return end; }
        }

        public void AddStop(Color color, float offset) {
            stops.Add(new Stop(color, offset));
            stops.Sort((s1, s2) => s1.Offset == s2.Offset ? 0 : (s1.Offset > s2.Offset ? 1 : -1));
        }

        public Color GetColor(float offset) {
            if (offset < start || offset > end) {
                throw new ArgumentException("offset is not within the gradient's bounds");
            }
            if (stops.Count == 0) {
                throw new Exception("no stops defined");
            }

            Stop s1 = null;
            Stop s2 = null;

            for (int x = 0; x < stops.Count; x++) {
                if (x == 0) {
                    s1 = stops[x];
                    s2 = stops[x];
                    if (offset <= s2.Offset) {
                        break;
                    }
                }
                else if (x == stops.Count - 1) {
                    s1 = stops[x];
                    s2 = stops[x];
                    if (offset >= s1.Offset) {
                        break;
                    }
                }
                if (offset >= stops[x].Offset && offset < stops[x + 1].Offset) {
                    s1 = stops[x];
                    s2 = stops[x + 1];
                    break;
                }
            }

            return Interpolate(s1.Color, s2.Color, s2.Offset == s1.Offset ? 0 : (offset - s1.Offset) / (s2.Offset - s1.Offset));
        }

        public IEnumerable<Color> GetGradient(int steps) {
            float factor = (end - start) / (steps - 1);
            for (int x = 0; x < steps; x++) {
                yield return GetColor(factor * x);
            }

        }

        public static Color Interpolate(Color c1, Color c2, float ratio) {
            if(ratio < 0 || ratio > 1) {
                throw new ArgumentException("ratio must be between 0 and 1");
            }
            float r1 = 1 - ratio;
            float r2 = ratio;
            return Color.FromArgb(
                (byte)(c1.A * r1 + c2.A * r2), 
                (byte)(c1.R * r1 + c2.R * r2),
                (byte)(c1.G * r1 + c2.G * r2),
                (byte)(c1.B * r1 + c2.B * r2));
        }
    }
}
