// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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
    /// Implementation of a dense matrix through a rectangular matrix array.
    /// This needs a lot of memory for large matrices; consider using a sparse
    /// implementation for huge matrices if the data permits exploiting sparsity.
    /// </summary>
    class ArrayMatrix<T> : IMatrix<T> {

        private int sizeX, sizeY;
        private T[,] matrix;

        /// <summary>
        /// Creates a rectangular matrix with the given 
        /// default value and size.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeY"></param>
        public ArrayMatrix(T defaultValue, int sizeX, int sizeY) {
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            matrix = new T[sizeX, sizeY];

            for (int x = 0; x < sizeX; x++) {
                for (int y = 0; y < sizeY; y++) {
                    matrix[x, y] = defaultValue;
                }
            }
        }

        /// <summary>
        /// Creates a square matrix (LengthX == LengthY) 
        /// with the given default value and size.
        /// </summary>
        public ArrayMatrix(T defaultValue, int size)
            : this(defaultValue, size, size) {
            //
        }

        public T this[int x, int y] {
            get {
                return matrix[x, y];
            }
            set {
                matrix[x, y] = value;
            }
        }

        public int LengthX {
            get { return sizeX; }
        }

        public int LengthY {
            get { return sizeY; }
        }
    }
}
