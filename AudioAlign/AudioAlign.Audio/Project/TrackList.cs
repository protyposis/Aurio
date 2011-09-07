using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace AudioAlign.Audio.Project {
    public class TrackList<T> : IEnumerable<T> where T : Track {
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

        public TrackList(IEnumerable<T> collection) {
            list = new List<T>(collection);
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

        /// <summary>
        /// Gets the time at which the earliest track in the tracklist starts.
        /// </summary>
        public TimeSpan Start {
            get {
                TimeSpan start = TimeSpan.Zero;
                foreach (T track in this) {
                    if (start == TimeSpan.Zero || track.Offset < start) {
                        start = track.Offset;
                    }
                }
                return start;
            }
        }

        /// <summary>
        /// Gets the time at which the latest track in the tracklist ends.
        /// </summary>
        public TimeSpan End {
            get {
                TimeSpan end = TimeSpan.Zero;
                foreach (T track in this) {
                    if (end == TimeSpan.Zero || track.Offset + track.Length > end) {
                        end = track.Offset + track.Length;
                    }
                }
                return end;
            }
        }

        #region IEnumerable<T> Members

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)list).GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return list.GetEnumerator();
        }

        #endregion
    }
}
