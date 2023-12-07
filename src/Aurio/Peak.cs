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

using System.Runtime.InteropServices;

namespace Aurio
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Peak
    {
        private float min,
            max;

        public Peak(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public float Min
        {
            get { return min; }
            set { min = value; }
        }

        public float Max
        {
            get { return max; }
            set { max = value; }
        }

        public void Merge(Peak p)
        {
            Merge(p.min, p.max);
        }

        public void Merge(float min, float max)
        {
            if (this.min > min)
            {
                this.min = min;
            }
            if (this.max < max)
            {
                this.max = max;
            }
        }

        public override string ToString()
        {
            return "Peak [" + min + ";" + max + "]";
        }
    }
}
