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

namespace Aurio.LibSampleRate
{
    internal class Interop32Wrapper : IInteropWrapper
    {
        #region IInteropWrapper Members

        public IntPtr src_new(ConverterType converter_type, int channels, out int error)
        {
            return Interop32.src_new(converter_type, channels, out error);
        }

        public IntPtr src_delete(IntPtr state)
        {
            return Interop32.src_delete(state);
        }

        public int src_process(IntPtr state, ref SRC_DATA data)
        {
            return Interop32.src_process(state, ref data);
        }

        public int src_reset(IntPtr state)
        {
            return Interop32.src_reset(state);
        }

        public int src_set_ratio(IntPtr state, double new_ratio)
        {
            return Interop32.src_set_ratio(state, new_ratio);
        }

        public int src_is_valid_ratio(double ratio)
        {
            return Interop32.src_is_valid_ratio(ratio);
        }

        public int src_error(IntPtr state)
        {
            return Interop32.src_error(state);
        }

        public string src_strerror(int error)
        {
            return Interop32.src_strerror(error);
        }

        #endregion
    }
}
