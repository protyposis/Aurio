using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace WaveCutter {
    public class Parameters {
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public ObservableCollection<FileInfo> SourceFiles { get; set; }
    }
}
