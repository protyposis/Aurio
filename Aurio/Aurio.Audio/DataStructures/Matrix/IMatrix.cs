using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.DataStructures.Matrix {
    /// <summary>
    /// Common interface for (sparse) matrix implementations.
    /// </summary>
    public interface IMatrix<T> {

        /// <summary>
        /// Gets or sets a value at the supplied coordinates.
        /// </summary>
        T this[int x, int y] { get; set; }

        /// <summary>
        /// Returns the length/width of the x-axis from zero 
        /// to the maximal x-coordinate of the stored values.
        /// </summary>
        int LengthX { get; }

        /// <summary>
        /// Returns the length/height of the y-axis from zero 
        /// to the maximal x-coordinate of the stored values.
        /// </summary>
        int LengthY { get; }
    }
}
