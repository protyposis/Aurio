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
        }

        public Track(FileInfo fileInfo, TimeSpan length, TimeSpan offset)
            : this(fileInfo) {
            this.Length = length;
            this.Offset = offset;
        }

        public static MediaType MediaType { get; protected set; }
        public TimeSpan Length { get; set; }
        public TimeSpan Offset { get; set; }
        public FileInfo FileInfo { get; private set; }
    }
}
