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

namespace Aurio.DataStructures.Matrix
{
    /// <summary>
    /// Implementation of a sparse matrix by using dictionaries.
    /// taken from: http://www.blackbeltcoder.com/Articles/algorithms/creating-a-sparse-matrix-in-net
    ///
    /// This matrix has a relatively high memory overhead. When storing lots of values that are locally
    /// aggregated in the sparse space, it is recommended to use a <see cref="PatchMatrix"/>.
    /// </summary>
    class SparseMatrix<T> : IMatrix<T>
    {
        // Master dictionary hold rows of column dictionary
        protected Dictionary<int, Dictionary<int, T>> _rows;

        private int numRows;
        private int numCols;

        /// <summary>
        /// Constructs a SparseMatrix instance.
        /// </summary>
        public SparseMatrix()
        {
            _rows = new Dictionary<int, Dictionary<int, T>>();
            numRows = 0;
            numCols = 0;
        }

        /// <summary>
        /// Gets or sets the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        public T this[int row, int col]
        {
            get { return GetAt(row, col); }
            set { SetAt(row, col, value); }
        }

        /// <summary>
        /// Gets the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        /// <returns>Value at the specified position</returns>
        private T GetAt(int row, int col)
        {
            Dictionary<int, T> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                T value = default(T);
                if (cols.TryGetValue(col, out value))
                    return value;
            }
            return default(T);
        }

        /// <summary>
        /// Sets the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        /// <param name="value">New value</param>
        private void SetAt(int row, int col, T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                // Remove any existing object if value is default(T)
                RemoveAt(row, col);
            }
            else
            {
                // Set value
                Dictionary<int, T> cols;
                if (!_rows.TryGetValue(row, out cols))
                {
                    cols = new Dictionary<int, T>();
                    _rows.Add(row, cols);
                    if (numRows < row)
                    {
                        numRows = row;
                    }
                    if (numCols < col)
                    {
                        numCols = col;
                    }
                }
                cols[col] = value;
            }
        }

        /// <summary>
        /// Removes the value at the specified matrix position.
        /// </summary>
        /// <param name="row">Matrix row</param>
        /// <param name="col">Matrix column</param>
        public void RemoveAt(int row, int col)
        {
            Dictionary<int, T> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                // Remove column from this row
                cols.Remove(col);
                // Remove entire row if empty
                if (cols.Count == 0)
                    _rows.Remove(row);
            }
        }

        /// <summary>
        /// Returns all items in the specified row.
        /// </summary>
        /// <param name="row">Matrix row</param>
        public IEnumerable<T> GetRowData(int row)
        {
            Dictionary<int, T> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                foreach (KeyValuePair<int, T> pair in cols)
                {
                    yield return pair.Value;
                }
            }
        }

        /// <summary>
        /// Returns the number of items in the specified row.
        /// </summary>
        /// <param name="row">Matrix row</param>
        public int GetRowDataCount(int row)
        {
            Dictionary<int, T> cols;
            if (_rows.TryGetValue(row, out cols))
            {
                return cols.Count;
            }
            return 0;
        }

        /// <summary>
        /// Returns all items in the specified column.
        /// This method is less efficent than GetRowData().
        /// </summary>
        /// <param name="col">Matrix column</param>
        /// <returns></returns>
        public IEnumerable<T> GetColumnData(int col)
        {
            foreach (KeyValuePair<int, Dictionary<int, T>> rowdata in _rows)
            {
                T result;
                if (rowdata.Value.TryGetValue(col, out result))
                    yield return result;
            }
        }

        /// <summary>
        /// Returns the number of items in the specified column.
        /// This method is less efficent than GetRowDataCount().
        /// </summary>
        /// <param name="col">Matrix column</param>
        public int GetColumnDataCount(int col)
        {
            int result = 0;

            foreach (KeyValuePair<int, Dictionary<int, T>> cols in _rows)
            {
                if (cols.Value.ContainsKey(col))
                    result++;
            }
            return result;
        }

        public int LengthX
        {
            get { return numRows; }
        }

        public int LengthY
        {
            get { return numCols; }
        }
    }
}
