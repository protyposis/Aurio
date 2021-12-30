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

namespace Aurio.Matching.Wang2003;

class WangProfile : DefaultProfile
{
    /// <summary>
    /// Fingerprinting configuration guessed/extrapolated from algorithm publications.
    /// </summary>
    /// <remarks>
    /// "The general Spectral Lp Norm is calculated at each time along the sound signal by calculating
    /// a short-time spectrum, for example via a Hanning-windowed Fast Fourier Transform (FFT). A preferred
    /// embodiment uses a samping rate of 8000 Hz, an FFT frame size of 1024 samples, and a stride of 64 samples
    /// for each time slice."
    /// "As before, a preferred embodiment uses a samping rate of 8000 Hz, an FFT frame size of 1024 samples,
    /// and a stride of 64 samples for each time slice."
    /// source: https://patents.google.com/patent/WO2002011123A2
    /// </remarks>
    public WangProfile()
    {
        Name = "Wang03 guessed";

        SamplingRate = 8000; // Paper figures (e.g. 1A) show a 4000 Hz range, patent mentions 8000 Hz sampling rate (see quotes above)
        WindowSize = 1024; // from patent (see quotes above)
        HopSize = 64; // from patent, hop size is sometimes called stride (see quotes above)
            
        var framesPerSecond = (double)SamplingRate / HopSize;
        var hzPerMagnitude = (double)SamplingRate / WindowSize;
            
        PeakFanout = 10; // paper talks about "F=10"
        TargetZoneDistance = (int)(2 * framesPerSecond); // extrapolated from paper figure 1C
        TargetZoneLength = (int)(5 * framesPerSecond); // extrapolated from paper figure 1C
        TargetZoneWidth = (int)(1400 / hzPerMagnitude); // extrapolated from paper figure 1C
    }
}
