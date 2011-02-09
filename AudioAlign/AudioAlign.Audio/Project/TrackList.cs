using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace AudioAlign.Audio.Project {
    public class TrackList<T> : IEnumerable where T : Track {
        private readonly List<T> list;

        public class TrackListEventArgs : EventArgs {

            public TrackListEventArgs(T track, int index) {
                this.Track = track;
                this.Index = index;
            }

            public T Track { get; private set; }
            public int Index { get; private set; }
        }

        public delegate void TrackListChangedEventHandler(object sender, TrackListEventArgs e);
        public event TrackListChangedEventHandler TrackAdded;
        public event TrackListChangedEventHandler TrackRemoved;

        public TrackList() {
            list = new List<T>();
        }

        private void OnTrackAdded(TrackListEventArgs e) {
            if (TrackAdded != null) {
                TrackAdded(this, e);
            }
        }

        private void OnTrackRemoved(TrackListEventArgs e) {
            if (TrackRemoved != null) {
                TrackRemoved(this, e);
            }
        }

        public void Add(T track) {
            list.Add(track);
            OnTrackAdded(new TrackListEventArgs(track, list.IndexOf(track)));
        }

        public bool Contains(T item) {
            return list.Contains(item);
        }

        public int Count {
            get { return list.Count; }
        }

        public bool Remove(T track) {
            if (list.Contains(track)) {
                int index = list.IndexOf(track);
                if (list.Remove(track)) {
                    OnTrackRemoved(new TrackListEventArgs(track, index));
                    return true;
                }
            }
            return false;
        }

        public T this[int index] {
            get { return list[index]; }
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)list).GetEnumerator();
        }

        #endregion
    }
}
