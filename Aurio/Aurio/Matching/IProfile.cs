using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching {
    public interface IProfile {
        string Name { get; }
        double HashTimeScale { get; }
    }
}
