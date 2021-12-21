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

namespace Aurio.DataStructures
{
    /// <summary>
    /// Base interface for average calculator implementations.
    /// </summary>
    interface IAverage
    {

        /// <summary>
        /// Adds a new value to the average calculator and returns the updated average.
        /// </summary>
        /// <param name="value">the new value to add</param>
        /// <returns>the updated average value</returns>
        float Add(float value);

        /// <summary>
        /// Gets the average value.
        /// </summary>
        float Average { get; }

        /// <summary>
        /// Clears the average.
        /// </summary>
        void Clear();
    }
}
