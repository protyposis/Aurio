using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    /// <summary>
    /// Implements a sparse matrix supporting tightly filled regions with minimal memory overhead.
    /// </summary>
    class PatchMatrix {

        private struct Mapping {
            public int x, y, xOffset, yOffset;
        }

        private double defaultValue;
        private int patchSizeX;
        private int patchSizeY;

        private int lengthX;
        private int lengthY;

        private SparseMatrix<double[,]> matrix;

        public PatchMatrix(double defaultValue, int patchSizeX, int patchSizeY) {
            this.defaultValue = defaultValue;
            this.patchSizeX = patchSizeX;
            this.patchSizeY = patchSizeY;
            matrix = new SparseMatrix<double[,]>();
        }

        public PatchMatrix(double defaultValue, int patchSize)
            : this(defaultValue, patchSize, patchSize) {
            //
        }

        public PatchMatrix(double defaultValue)
            : this(defaultValue, 50) {
            //
        }

        public PatchMatrix()
            : this(0d, 50) {
            //
        }

        public double this[int x, int y] {
            get {
                Mapping m = CalculateMapping(x, y);
                double[,] patch = GetPatch(m, false);
                if (patch == null) {
                    return defaultValue;
                }
                return patch[m.xOffset, m.yOffset];
            }
            set {
                Mapping m = CalculateMapping(x, y);
                double[,] patch = GetPatch(m, true);
                patch[m.xOffset, m.yOffset] = value;
                if (x > lengthX) {
                    lengthX = x;
                }
                if (y > lengthY) {
                    lengthY = y;
                }
            }
        }

        public int LengthX {
            get { return lengthX + 1; }
        }

        public int LengthY {
            get { return lengthY + 1; }
        }

        public double[,] CopyToArray(int x, int y, int width, int height) {
            throw new NotImplementedException();
        }

        private Mapping CalculateMapping(int x, int y) {
            Mapping m = new Mapping();
            m.xOffset = x % patchSizeX;
            m.yOffset = y % patchSizeY;
            m.x = x - m.xOffset;
            m.y = y - m.yOffset;
            return m;
        }

        private double[,] GetPatch(Mapping m, bool createMissing) {
            double[,] patch = matrix[m.x, m.y];
            if (patch == null) {
                if (createMissing) {
                    patch = new double[patchSizeX, patchSizeY];
                    for (int i = 0; i < patchSizeX; i++) {
                        for (int j = 0; j < patchSizeY; j++) {
                            patch[i, j] = defaultValue;
                        }
                    }
                    matrix[m.x, m.y] = patch;
                }
                else {
                    return null;
                }
            }
            return patch;
        }
    }
}
