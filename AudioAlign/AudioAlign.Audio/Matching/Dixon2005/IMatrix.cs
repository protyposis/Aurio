using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    public interface IMatrix {
        double this[int x, int y] { get; set; }
        int LengthX { get; }
        int LengthY { get; }
    }
}
