using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    public enum ChromaMappingMode {
        /// <summary>
        /// The type of frequency bin to chroma mapping described in
        /// - Bartsch, Mark A., and Gregory H. Wakefield. "Audio thumbnailing of popular music 
        ///   using chroma-based representations." Multimedia, IEEE Transactions on 7.1 (2005): 96-104.
        /// </summary>
        Paper,
        /// <summary>
        /// The type of frequency bin to chroma mapping applied by Chromaprint.
        /// </summary>
        Chromaprint
    }
}
