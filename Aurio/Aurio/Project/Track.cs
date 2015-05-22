// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace Aurio.Project {
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
