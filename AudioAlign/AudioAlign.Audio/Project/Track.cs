using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AudioAlign.Audio.Project {
    public abstract class Track {

        public Track(FileInfo fileInfo) {
            if (!fileInfo.Exists) {
                throw new ArgumentException("the specified file does not exist");
            }
            this.FileInfo = fileInfo;
            this.Name = fileInfo.Name;
        }

        public static MediaType MediaType { get; protected set; }
        public TimeSpan Length { get; set; }
        public TimeSpan Offset { get; set; }
        public FileInfo FileInfo { get; private set; }
        public string Name { get; set; }
    }
}
