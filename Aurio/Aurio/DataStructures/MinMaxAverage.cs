using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.DataStructures
{
    public class MinMaxAverage
    {
        // Collects values for average calculation
        private readonly List<long> _values;
        private long _min;
        private long _max;

        public MinMaxAverage()
        {
            _values = new List<long>();
            Reset();
        }

        public void Add(long value)
        {
            _values.Add(value);

            if (value < _min)
            {
                _min = value;
            }

            if (value > _max)
            {
                _max = value;
            }
        }

        public long Min
        {
            get { return _min; }
        }

        public long Max
        {
            get { return _max; }
        }

        public long Average
        {
            get { return _values.Sum() / _values.Count; }
        }

        public int SampleCount
        {
            get { return _values.Count; }
        }

        public Boolean SamplesCollected
        {
            get { return _values.Count > 0; }
        }

        public void Reset()
        {
            _values.Clear();
            _min = long.MaxValue;
            _max = long.MinValue;
        }
    }
}
