using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Aurio.Audio.Project {
    public class VideoTrack : Track {

        public VideoTrack(FileInfo fileInfo)
            : base(fileInfo) {
        }

        public override MediaType MediaType {
            get { return MediaType.Video; }
        }

        public override string ToString() {
            return "Video" + base.ToString();
        }
    }
}
