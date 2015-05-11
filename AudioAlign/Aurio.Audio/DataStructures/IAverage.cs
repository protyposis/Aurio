using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.DataStructures {
    /// <summary>
    /// Base interface for average calculator implementations.
    /// </summary>
    interface IAverage {

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
