﻿//
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

namespace Aurio.LibSampleRate
{
    internal interface IInteropWrapper
    {
        IntPtr src_new(ConverterType converter_type, int channels, out int error);
        IntPtr src_delete(IntPtr state);
        int src_process(IntPtr state, ref SRC_DATA data);
        int src_reset(IntPtr state);
        int src_set_ratio(IntPtr state, double new_ratio);
        int src_is_valid_ratio(double ratio);
        int src_error(IntPtr state);
        string src_strerror(int error);
    }
}
