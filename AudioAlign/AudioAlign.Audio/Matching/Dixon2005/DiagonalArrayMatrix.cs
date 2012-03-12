using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    /// <summary>
    /// Stores a diagonal matrix with a max diagonal height as a square matrix with column offsets.
    /// 
    /// Example:
    /// 
    /// - - x
    /// - x -
    /// x - -
    /// (diagonal matrix)
    /// 
    /// x x x
    /// 0 1 2
    /// (square matrix with offsets)
    /// 
    /// When a new column is written into the matrix, always the value with the lowest Y-index must be 
    /// written first, because it determines the column offset.
    /// </summary>
    public class DiagonalArrayMatrix : IMatrix {

        private int maxColumnHeight;
        private double defaultValue;
        private List<double[]> columns;
        private List<int> offsets;

        private int maxY; // store the maximal set Y index needed for the LengthY property

        public DiagonalArrayMatrix(int maxColumnHeight, double defaultValue) {
            this.maxColumnHeight = maxColumnHeight;
            this.defaultValue = defaultValue;

            columns = new List<double[]>();
            offsets = new List<int>();
        }

        public double this[int x, int y] {
            get {
                double[] col;
                int offset;

                if (x < columns.Count) {
                    col = columns[x];
                    offset = offsets[x];
                }
                else {
                    return defaultValue;
                }

                if (y < offset || y >= offset + col.Length) {
                    return defaultValue;
                }

                return col[y - offset];
            }
            set {
                double[] col;
                int offset;

                if (x == columns.Count) {
                    col = new double[maxColumnHeight];
                    for (int i = 0; i < col.Length; i++) {
                        col[i] = defaultValue;
                    }
                    columns.Add(col);
                    offsets.Add(y);
                    offset = y;

                    if (maxY < y) {
                        maxY = y;
                    }
                }
                else if (x < columns.Count) {
                    col = columns[x];
                    offset = offsets[x];
                }
                else {
                    throw new NotSupportedException();
                }

                col[y - offset] = value;
            }
        }

        public int LengthX {
            get { return columns.Count; }
        }

        public int LengthY {
            get { return maxY + 1; }
        }
    }
}
