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

namespace Aurio.DataStructures.Matrix {
    /// <summary>
    /// Implements a sparse matrix supporting densely filled regions with minimal memory overhead.
    /// 
    /// This implementation works by dividing the sparse matrix space into dense local areas (patches).
    /// The sparse "global" matrix is implemented through an instance of the <see cref="SparseMatrix"/>,
    /// the "local" patches are matrix arrays.
    /// </summary>
    class PatchMatrix<T> : IMatrix<T> {

        private struct Mapping {
            public int x, y, xOffset, yOffset;
        }

        private T defaultValue;
        private int patchSizeX;
        private int patchSizeY;

        private int lengthX;
        private int lengthY;

        private SparseMatrix<T[,]> matrix;

        /// <summary>
        /// Creates a new matrix and initializes it with the given default value.
        /// The patches of the matrix will be sized according to the supplied square size.
        /// </summary>
        public PatchMatrix(T defaultValue, int patchSizeX, int patchSizeY) {
            this.defaultValue = defaultValue;
            this.patchSizeX = patchSizeX;
            this.patchSizeY = patchSizeY;
            matrix = new SparseMatrix<T[,]>();
        }

        /// <summary>
        /// Creates a new matrix and initializes it with the given default value.
        /// The patches of the matrix will be sized according to the supplied square size.
        /// </summary>
        public PatchMatrix(T defaultValue, int patchSize)
            : this(defaultValue, patchSize, patchSize) {
            //
        }

        public PatchMatrix(T defaultValue)
            : this(defaultValue, 50) {
            //
        }

        public T this[int x, int y] {
            get {
                Mapping m = CalculateMapping(x, y);
                T[,] patch = GetPatch(m, false);
                if (patch == null) {
                    return defaultValue;
                }
                return patch[m.xOffset, m.yOffset];
            }
            set {
                Mapping m = CalculateMapping(x, y);
                T[,] patch = GetPatch(m, true);
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

        /// <summary>
        /// Calculates, for a given x/y coordinate, a mapping containing
        /// the root coordinates of the patch that this coordinate belongs
        /// to, and the offsets inside this patch.
        /// </summary>
        private Mapping CalculateMapping(int x, int y) {
            Mapping m = new Mapping();
            m.xOffset = x % patchSizeX;
            m.yOffset = y % patchSizeY;
            m.x = x - m.xOffset;
            m.y = y - m.yOffset;
            return m;
        }

        /// <summary>
        /// Gets a patch for a given mapping, and optionally creates and initializes
        /// it if not existing yet.
        /// </summary>
        private T[,] GetPatch(Mapping m, bool createMissing) {
            T[,] patch = matrix[m.x, m.y];
            if (patch == null) {
                if (createMissing) {
                    patch = new T[patchSizeX, patchSizeY];
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
