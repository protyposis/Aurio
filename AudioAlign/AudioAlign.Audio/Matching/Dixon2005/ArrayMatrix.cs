using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    class ArrayMatrix : IMatrix {

        private int sizeX, sizeY;
        private double[,] matrix;

        public ArrayMatrix(double defaultValue, int sizeX, int sizeY) {
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            matrix = new double[sizeX, sizeY];

            for (int x = 0; x < sizeX; x++) {
                for (int y = 0; y < sizeY; y++) {
                    matrix[x, y] = defaultValue;
                }
            }
        }

        public ArrayMatrix(double defaultValue, int size)
            : this(defaultValue, size, size) {
            //
        }

        public double this[int x, int y] {
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
