using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching {
    /// <summary>
    /// The method with which a match will be chosen from a collection of matches
    /// </summary>
    public enum MatchFilterMode {
        None,
        /// <summary>
        /// Pick out the best match
        /// </summary>
        Best,
        /// <summary>
        /// Pick out the temporally first match
        /// </summary>
        First,
        /// <summary>
        /// Pick out the temporally mid match
        /// </summary>
        Mid,
        /// <summary>
        /// Pick out the temporally last match
        /// </summary>
        Last
    }
}
