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
    /// A simple moving average calculator.
    /// http://en.wikipedia.org/wiki/Moving_average#Simple_moving_average
    /// </summary>
    class SimpleMovingAverage : IAverage
    {
        public float sum;
        public RingBuffer<float> history;

        /// <summary>
        /// Creates a new running average over the specified amount of values.
        /// </summary>
        /// <param name="length">the number of values to average over</param>
        public SimpleMovingAverage(int length)
        {
            history = new RingBuffer<float>(length);
            Clear();
        }

        /// <summary>
        /// Adds a new value to the average and returns the updated average.
        /// </summary>
        /// <param name="value">the new value to add</param>
        /// <returns>the average value updated with the supplied value</returns>
        public float Add(float value)
        {
            // When the history is already full, the oldest value needs to be removed
            // from the sum before the newest addition is added.
            if (history.Count == history.Length)
            {
                sum -= history[0];
            }

            // Update with new value
            sum += value;
            history.Add(value);

            return Average;
        }

        /// <summary>
        /// Gets the average value.
        /// </summary>
        public float Average
        {
            get { return history.Count == 0 ? 0 : sum / history.Count; }
        }

        /// <summary>
        /// Returns the number of values that are part of the average.
        /// After instantiation of this class, the count is zero as no values
        /// have been added. This number then rises up until the length
        /// specified in the constructor.
        /// </summary>
        public int Count
        {
            get { return history.Count; }
        }

        /// <summary>
        /// Gets the number of values that are kept for averaging.
        /// </summary>
        public int Length
        {
            get { return history.Length; }
        }

        /// <summary>
        /// Clears the average.
        /// </summary>
        public void Clear()
        {
            sum = 0;
            history.Clear();
        }
    }
}
