using System.Collections.ObjectModel;
using System.IO;

namespace WaveCutter
{
    public class Parameters
    {
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public ObservableCollection<FileInfo> SourceFiles { get; set; }
    }
}
