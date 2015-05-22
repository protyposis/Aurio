// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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
using Aurio.Streams;

namespace Aurio.Features.ContinuousFrequencyActivation {
    class StrongPeakDetector : FrequencyActivationCalculator {

        private enum Direction {
            Up,
            Down
        }

        private float[] frequencyActivation; // buffer
        private Direction direction;

        public StrongPeakDetector(IAudioStream stream)
            : base(stream) {
                frequencyActivation = new float[WindowSize / 2];
        }

        public override void ReadFrame(float[] strongPeakValues) {
            base.ReadFrame(frequencyActivation);

            // scan peak values for local min/maxima
            int xl = 0; // left local minimum index
            int xp = 0; // max local peak index
            int xr = 0; // right local minimum index
            int peakCount = 0; // index counter for the output array

            Array.Clear(strongPeakValues, 0, strongPeakValues.Length);

            direction = frequencyActivation[1] >= frequencyActivation[0] ? Direction.Up : Direction.Down;

            for (int i = 1; i < frequencyActivation.Length; i++) {
                if (frequencyActivation[i] < frequencyActivation[i - 1] && direction == Direction.Up) {
                    // local maximum found
                    xp = i - 1;

                    direction = Direction.Down;
                }
                else if (frequencyActivation[i] >= frequencyActivation[i - 1] && direction == Direction.Down) {
                    // local minimum found
                    xl = xr;
                    xr = i - 1;

                    if (xl > 0 && xp > 0) {
                        // local l-min, max and r-min found, calculate peak value
                        float hl = frequencyActivation[xp] - frequencyActivation[xl];
                        float hr = frequencyActivation[xp] - frequencyActivation[xr];
                        float height = Math.Min(hl, hr);
                        float width = hl < hr ? xp - xl : xr - xp;
                        strongPeakValues[peakCount++] = height / width;
                    }

                    direction = Direction.Up;
                }
            }




            //for (int i = 1; i < frequencyActivation.Length; i++) {
            //    if (frequencyActivation[i] < frequencyActivation[i - 1]) {
            //        // local max found
            //        xp = i - 1;
            //    }
            //    else if (frequencyActivation[i] > frequencyActivation[i - 1]) {
            //        // local min found
            //        xl = xr;
            //        xr = i - 1;

            //        if (xl > 0 && xp > 0) {
            //            // local l-min, max and r-min found, calculate peak value
            //            float hl = frequencyActivation[xp] - frequencyActivation[xl];
            //            float hr = frequencyActivation[xp] - frequencyActivation[xr];
            //            float height = Math.Min(hl, hr);
            //            float width = hl < hr ? xp - xl : xr - xp;
            //            strongPeakValues[peakCount++] = height / width;
            //        }
            //    }
            //}

            /* HACK store peak count in last array element 
             * it cannot happen that every element is a peak value so the last element will never be reached */
            strongPeakValues[strongPeakValues.Length - 1] = peakCount;
        }
    }
}
