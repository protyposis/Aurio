using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace AudioAlign.Audio.Project {
    public abstract class Track : INotifyPropertyChanged {

        private TimeSpan length = TimeSpan.Zero;
        private TimeSpan offset = TimeSpan.Zero;
        private string name;

        public Track(FileInfo fileInfo) {
            if (!fileInfo.Exists) {
                throw new ArgumentException("the specified file does not exist");
            }
            this.FileInfo = fileInfo;
            this.Name = fileInfo.Name;
        }

        public static MediaType MediaType { get; protected set; }

        public TimeSpan Length {
            get { return length; }
            set { length = value; OnPropertyChanged("Length"); }
        }

        public TimeSpan Offset {
            get { return offset; }
            set { offset = value; OnPropertyChanged("Offset"); }
        }

        public FileInfo FileInfo { get; private set; }

        public string Name {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}
