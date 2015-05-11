using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.DataStructures.Matrix {
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
