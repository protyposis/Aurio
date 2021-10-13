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

namespace Aurio {
    public struct Interval: IEquatable<Interval> {
        public static readonly Interval None = new Interval(-1, -1);

        private long from, to;

        public Interval(long from, long to) {
            this.from = from;
            this.to = to;
        }

        public Interval(TimeSpan from, TimeSpan to)
        {
            this.from = from.Ticks;
            this.to = to.Ticks;
        }

        public long From {
            get { return from; }
            set { from = value; }
        }

        public long To {
            get { return to; }
            set { to = value; }
        }

        public long Length {
            get { return to - from; }
        }

        public bool Intersects(Interval interval) {
            return !(from >= interval.to || to <= interval.from);
        }

        public Interval Intersect(Interval interval) {
            if(!Intersects(interval)) {
                return None;
            }
            return new Interval(Math.Max(from, interval.from), Math.Min(to, interval.to));
        }

        public bool Contains(long value) {
            return value >= from && value <= to;
        }

        public void Offset(long offset) {
            from += offset;
            to += offset;
        }

        public void Divide(long at, out Interval left, out Interval right) {
            if (at <= from) {
                left = new Interval(from, from);
                right = new Interval(from, to);
            }
            else if (at >= to) {
                left = new Interval(from, to);
                right = new Interval(to, to);
            }
            else {
                left = new Interval(from, at);
                right = new Interval(at, to);
            }
        }

        #region IEquatable<Interval> Members

        public bool Equals(Interval other) {
            return from == other.from && to == other.to;
        }

        public override bool Equals(object obj) {
            return Equals((Interval)obj);
        }

        public override int GetHashCode() {
            return (int)from ^ (int)(from >> 32) ^ (int)to ^ (int)(to >> 32);
        }

        #endregion

        public static Interval operator -(Interval interval, long scalar) {
            return new Interval(interval.from - scalar, interval.to - scalar);
        }

        public static Interval operator +(Interval interval, long scalar) {
            return new Interval(interval.from + scalar, interval.to + scalar);
        }

        public static Interval operator *(Interval interval, long scalar) {
            return new Interval(interval.from * scalar, interval.to * scalar);
        }

        public static Interval operator /(Interval interval, long scalar) {
            return new Interval(interval.from / scalar, interval.to / scalar);
        }

        public static bool operator <(Interval interval, long scalar) {
            return scalar < interval.From;
        }

        public static bool operator >(Interval interval, long scalar) {
            return scalar > interval.From;
        }

        public static bool operator <=(Interval interval, long scalar) {
            return scalar <= interval.From;
        }

        public static bool operator >=(Interval interval, long scalar) {
            return scalar >= interval.From;
        }

        public static bool operator <(long scalar, Interval interval) {
            return scalar < interval.From;
        }

        public static bool operator >(long scalar, Interval interval) {
            return scalar > interval.From;
        }

        public static bool operator <=(long scalar, Interval interval) {
            return scalar <= interval.From;
        }

        public static bool operator >=(long scalar, Interval interval) {
            return scalar >= interval.From;
        }

        public static bool operator ==(Interval interval1, Interval interval2) {
            return interval1.Equals(interval2);
        }

        public static bool operator !=(Interval interval1, Interval interval2) {
            return !interval1.Equals(interval2);
        }

        public TimeSpan TimeFrom {
            get { return new TimeSpan(From); }
        }

        public TimeSpan TimeTo {
            get { return new TimeSpan(To); }
        }

        public TimeSpan TimeLength {
            get { return new TimeSpan(Length); }
        }

        public override string ToString() {
            return "[" + TimeFrom + ";" + TimeTo + ";" + TimeLength + "]";
        }
    }
}
