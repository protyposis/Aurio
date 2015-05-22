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
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Aurio.Streams {
    public class TimeWarpCollection : ObservableCollection<TimeWarp> {

        private bool sorting = false;
        private bool rangeAdding = false;

        public TimeWarpCollection() {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if ((sorting && e.Action == NotifyCollectionChangedAction.Move) || 
                (rangeAdding && e.Action == NotifyCollectionChangedAction.Add)) {
                // suppress change event if it is triggered by the sort method or a range add
                return;
            }

            base.OnCollectionChanged(e);
            Sort();
            ValidateMappings();
        }

        public void AddRange(IEnumerable<TimeWarp> range) {
            rangeAdding = true;
            foreach (TimeWarp timeWarp in range) {
                Add(timeWarp);
            }
            rangeAdding = false;

            Sort();
            ValidateMappings();

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, new List<TimeWarp>(range)));
        }

        /// <summary>
        /// Sorts the collection according to the mappings' source time.
        /// Code taken and adapted from: http://kiwigis.blogspot.com/2010/03/how-to-sort-obversablecollection.html
        /// </summary>
        public void Sort() {
            sorting = true;
            // do a simple bubble sort
            // there probably won't ever be that much elements in this collection that the bubble sort will be a performance hit
            for (int i = Count - 1; i >= 0; i--) {
                for (int j = 1; j <= i; j++) {
                    TimeWarp o1 = this[j - 1];
                    TimeWarp o2 = this[j];
                    if (o1.From > o2.From) {
                        MoveItem(j - 1, j);
                    }
                }
            }
            sorting = false;
        }

        public new void Clear() {
            if (Count > 0) {
                base.Clear();
            }
        }

        public void ValidateMappings() {
            // validate that no mapping is overlapping with another one
            for (int x = 0; x < Count - 1; x++) {
                for (int y = x + 1; y < Count; y++) {
                    if (this[x].To > this[y].To) {
                        throw new Exception(this[x] + " is overlapping " + this[y]);
                    }
                    else if (!ResamplingStream.CheckSampleRateRatio(TimeWarp.CalculateSampleRateRatio(this[x], this[y]))) {
                        throw new Exception("invalid sample ratio: " + TimeWarp.CalculateSampleRateRatio(this[x], this[y]));
                    }
                }
            }
        }

        public void GetBoundingMappingsForSourcePosition(TimeSpan sourcePosition,
                out TimeWarp lowerMapping, out TimeWarp upperMapping) {
            lowerMapping = null;
            upperMapping = null;
            for (int x = 0; x < Count; x++) {
                if (sourcePosition < this[x].From) {
                    if (x == 0) {
                        upperMapping = this[x];
                        break;
                    }
                    else {
                        lowerMapping = this[x - 1];
                        upperMapping = this[x];
                        break;
                    }
                }
                else if(x == Count - 1) {
                    lowerMapping = this[x];
                    break;
                }
            }
        }

        public void GetBoundingMappingsForWarpedPosition(TimeSpan warpedPosition,
                out TimeWarp lowerMapping, out TimeWarp upperMapping) {
            lowerMapping = null;
            upperMapping = null;
            for (int x = 0; x < Count; x++) {
                if (warpedPosition < this[x].To) {
                    if (x == 0) {
                        upperMapping = this[x];
                        break;
                    }
                    else {
                        lowerMapping = this[x - 1];
                        upperMapping = this[x];
                        break;
                    }
                }
                else if (x == Count - 1) {
                    lowerMapping = this[x];
                    break;
                }
            }
        }

        public TimeSpan TranslateSourceToWarpedPosition(TimeSpan sourcePosition) {
            TimeWarp lowerMapping;
            TimeWarp upperMapping;
            GetBoundingMappingsForSourcePosition(sourcePosition, out lowerMapping, out upperMapping);

            if (lowerMapping == null) {
                // position is before the first mapping -> linear adjust
                return sourcePosition + upperMapping.Offset;
            }
            else if (upperMapping == null) {
                // position is after the last mapping -> linear adjust
                return sourcePosition + lowerMapping.Offset;
            }
            else {
                return lowerMapping.To +
                    new TimeSpan((long)((sourcePosition - lowerMapping.From).Ticks *
                    TimeWarp.CalculateSampleRateRatio(lowerMapping, upperMapping)));
            }
        }
    }
}
