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
using System.Collections.Generic;
using Aurio.Project;

namespace Aurio.Matching.Wang2003
{
    public class FrameProcessedEventArgs : EventArgs
    {
        public AudioTrack AudioTrack { get; set; }
        public int Index { get; set; }
        public int Indices { get; set; }
        public float[] Spectrum { get; set; }
        public float[] SpectrumResidual { get; set; }
        public List<Peak> Peaks { get; set; }
    }
}
