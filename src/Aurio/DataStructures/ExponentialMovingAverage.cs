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

namespace Aurio.DataStructures
{
    /// <summary>
    /// An exponential moving average calculator.
    /// http://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average
    /// </summary>
    class ExponentialMovingAverage : IAverage
    {
        private float alpha;
        private long count;
        private float average;

        /// <summary>
        /// Creates a new exponential moving average calculator with the supplied weighting coefficient.
        /// </summary>
        /// <param name="alpha">the weighting coefficient</param>
        public ExponentialMovingAverage(float alpha)
        {
            this.alpha = alpha;
        }

        /// <summary>
        /// Adds a new value to the average calculator and returns the updated average.
        /// </summary>
        /// <param name="value">the new value to add to the average</param>
        /// <returns>the updated average</returns>
        public float Add(float value)
        {
            if (count++ == 0)
            { // The first value initializes the average
                average = value;
            }
            else
            { // All subsequent values trigger the recursive weighting function
                average = UpdateMovingAverage(average, alpha, value);
            }
            return Average;
        }

        /// <summary>
        /// Gets the current average value.
        /// </summary>
        public float Average
        {
            get { return count == 0 ? 0 : average; }
        }

        /// <summary>
        /// Clears the average calculator.
        /// </summary>
        public void Clear()
        {
            count = 0;
        }

        /// <summary>
        /// Updates an exponential moving average value by the supplied alpha with the supplied value.
        /// </summary>
        /// <param name="average">the current moving average value</param>
        /// <param name="alpha">the weighting coefficient</param>
        /// <param name="value">the new value to add to the average</param>
        /// <returns>the updated moving average value</returns>
        public static float UpdateMovingAverage(float average, float alpha, float value)
        {
            return alpha * value + (1 - alpha) * average;
        }
    }
}
