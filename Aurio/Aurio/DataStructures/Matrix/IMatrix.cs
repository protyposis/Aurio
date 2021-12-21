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
    /// Common interface for (sparse) matrix implementations.
    /// </summary>
    public interface IMatrix<T>
    {

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
