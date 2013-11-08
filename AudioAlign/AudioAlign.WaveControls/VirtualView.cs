using AudioAlign.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.WaveControls
{
    interface VirtualView
    {
        long VirtualViewportOffset { get; set; }
        long VirtualViewportWidth { get; set; }
        long VirtualViewportMinWidth { get; set; }
        long VirtualViewportMaxWidth { get; set; }
        Interval VirtualViewportInterval { get; }
    }
}
