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

namespace Aurio.DataStructures.Matrix
{
    /// <summary>
    /// Stores a diagonal matrix with a max diagonal height as a square matrix with column offsets.
    /// http://en.wikipedia.org/wiki/Diagonal_matrix
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
    class DiagonalArrayMatrix<T> : IMatrix<T>
    {
        private int maxColumnHeight;
        private T defaultValue;
        private List<T[]> columns;
        private List<int> offsets;

        private int maxY; // store the maximal set Y index needed for the LengthY property

        public DiagonalArrayMatrix(int maxColumnHeight, T defaultValue)
        {
            this.maxColumnHeight = maxColumnHeight;
            this.defaultValue = defaultValue;

            columns = new List<T[]>();
            offsets = new List<int>();
        }

        public T this[int x, int y]
        {
            get
            {
                T[] col;
                int offset;

                if (x < columns.Count)
                {
                    col = columns[x];
                    offset = offsets[x];
                }
                else
                {
                    return defaultValue;
                }

                if (y < offset || y >= offset + col.Length)
                {
                    return defaultValue;
                }

                return col[y - offset];
            }
            set
            {
                T[] col;
                int offset;

                if (x == columns.Count)
                {
                    col = new T[maxColumnHeight];
                    for (int i = 0; i < col.Length; i++)
                    {
                        col[i] = defaultValue;
                    }
                    columns.Add(col);
                    offsets.Add(y);
                    offset = y;
                }
                else if (x < columns.Count)
                {
                    col = columns[x];
                    offset = offsets[x];
                }
                else
                {
                    throw new NotSupportedException();
                }

                col[y - offset] = value;
                if (maxY < y)
                {
                    maxY = y;
                }
            }
        }

        public int LengthX
        {
            get { return columns.Count; }
        }

        public int LengthY
        {
            get { return maxY + 1; }
        }
    }
}
