using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace AudioAlign.Audio.Project {
    public abstract class Track : INotifyPropertyChanged {

        public static readonly string DEFAULT_COLOR = "#FF6495ED";

        public event EventHandler<ValueEventArgs<TimeSpan>> LengthChanged;
        public event EventHandler<ValueEventArgs<TimeSpan>> OffsetChanged;
        public event EventHandler<ValueEventArgs<string>> NameChanged;
        public event EventHandler<ValueEventArgs<string>> ColorChanged;
        public event EventHandler<ValueEventArgs<bool>> LockedChanged;

        private TimeSpan length = TimeSpan.Zero;
        private TimeSpan offset = TimeSpan.Zero;
        private string name;
        private string color = DEFAULT_COLOR;
        private bool locked = false;

        public Track(FileInfo fileInfo) {
            if (!fileInfo.Exists) {
                throw new ArgumentException("the specified file does not exist");
            }
            this.FileInfo = fileInfo;
            this.Name = fileInfo.Name;
        }

        public abstract MediaType MediaType { get; }

        public TimeSpan Length {
            get { return length; }
            set { length = value; OnLengthChanged(); }
        }

        public TimeSpan Offset {
            get { return offset; }
            set { offset = value; OnOffsetChanged(); }
        }

        public FileInfo FileInfo { get; private set; }

        public string Name {
            get { return name; }
            set { name = value; OnNameChanged(); }
        }

        public string Color {
            get { return color; }
            set { color = value; OnColorChanged(); }
        }

        /// <summary>
        /// Gets or sets a value telling if this track is locked, which means it cannot be manipulated in the timeline (position change, etc...).
        /// </summary>
        public bool Locked {
            get { return locked; } 
            set { locked = value; OnLockedChanged(); }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        private void OnLengthChanged() {
            if (LengthChanged != null) {
                LengthChanged(this, new ValueEventArgs<TimeSpan>(length));
            }
            OnPropertyChanged("Length");
        }

        private void OnOffsetChanged() {
            if (OffsetChanged != null) {
                OffsetChanged(this, new ValueEventArgs<TimeSpan>(offset));
            }
            OnPropertyChanged("Offset");
        }

        private void OnNameChanged() {
            if (NameChanged != null) {
                NameChanged(this, new ValueEventArgs<string>(name));
            }
            OnPropertyChanged("Name");
        }

        private void OnColorChanged() {
            if (ColorChanged != null) {
                ColorChanged(this, new ValueEventArgs<string>(color));
            }
            OnPropertyChanged("Color");
        }

        private void OnLockedChanged() {
            if (LockedChanged != null) {
                LockedChanged(this, new ValueEventArgs<bool>(locked));
            }
            OnPropertyChanged("Locked");
        }

        public override string ToString() {
            return "Track {" + GetHashCode() + " / " + name + " / " + length + " / " + offset + "}";
        }
    }
}
