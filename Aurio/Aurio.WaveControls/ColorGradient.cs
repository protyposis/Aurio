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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Aurio.WaveControls {
    public class ColorGradient {

        public class Stop {

            private Color color;
            private double offset;

            public Stop(Color color, double offset) {
                this.color = color;
                this.offset = offset;
            }

            public Color Color {
                get { return color; }
            }

            public double Offset {
                get { return offset; }
            }

            public override string ToString() {
                return "GradientStop{color=" + color + ";offset=" + offset + "}";
            }
        }

        private double start;
        private double end;
        private List<Stop> stops;

        public ColorGradient(double start, double end) {
            if (start > end) {
                throw new ArgumentException("start must be <= end");
            }
            this.start = start;
            this.end = end;
            this.stops = new List<Stop>();
        }

        public double Start {
            get { return start; }
        }

        public double End {
            get { return end; }
        }

        public void AddStop(Color color, double offset) {
            stops.Add(new Stop(color, offset));
            stops.Sort((s1, s2) => s1.Offset == s2.Offset ? 0 : (s1.Offset > s2.Offset ? 1 : -1));
        }

        public Color GetColor(double offset) {
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
            double factor = (end - start) / (steps - 1);
            for (int x = 0; x < steps; x++) {
                yield return GetColor(factor * x);
            }
        }

        public int[] GetGradientArgbArray(int steps) {
            return GetGradient(steps).Select(c => ColorGradient.ColorToArgb(c)).ToArray();
        }

        /// <summary>
        /// Returns a linearly interpolated color between the two given colors. The ratio can be between 0 
        /// and 1 and specifies how much of each color will be taken into the new color. A ratio of 0 means
        /// that the first color will be returned, a ratio of 1 means that the second color will be returned.
        /// All other ratios result in a mixed color.
        /// </summary>
        /// <param name="c1">the first color</param>
        /// <param name="c2">the second color</param>
        /// <param name="ratio">the mixing ration</param>
        /// <returns>a color mixed of both input colors according to the ratio</returns>
        public static Color Interpolate(Color c1, Color c2, double ratio) {
            if(ratio < 0 || ratio > 1) {
                throw new ArgumentException("ratio must be between 0 and 1");
            }
            double r1 = 1 - ratio;
            double r2 = ratio;
            return Color.FromArgb(
                (byte)Math.Round(c1.A * r1 + c2.A * r2),
                (byte)Math.Round(c1.R * r1 + c2.R * r2),
                (byte)Math.Round(c1.G * r1 + c2.G * r2),
                (byte)Math.Round(c1.B * r1 + c2.B * r2));
        }

        public static int ColorToArgb(Color c) {
            return c.A << 24 | c.R << 16 | c.G << 8 | c.B;
        }

        public static Color SetAlpha(Color c, byte alpha) {
            c.A = alpha;
            return c;
        }
    }
}
