using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Collections.Specialized;

namespace AudioAlign.Audio.Project {
    public class TrackList<T> : IEnumerable<T>, INotifyPropertyChanged, INotifyCollectionChanged where T : Track {
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
            OnTrackListChanged();
        }

        private void OnTrackRemoved(TrackListEventArgs e) {
            if (TrackRemoved != null) {
                TrackRemoved(this, e);
            }
            OnTrackListChanged();
        }

        public void Add(T track) {
            list.Add(track);
            OnTrackAdded(new TrackListEventArgs(track, list.IndexOf(track)));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, track));
        }

        public void Add(IEnumerable<T> tracks) {
            foreach (T track in tracks) {
                list.Add(track);
                OnTrackAdded(new TrackListEventArgs(track, list.IndexOf(track)));
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, tracks));
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
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, track, index));
                    return true;
                }
            }
            return false;
        }

        public T this[int index] {
            get { return list[index]; }
        }

        public int IndexOf(T element) {
            for(int i = 0; i < list.Count; i++) {
                if (list[i] == element) {
                    return i;
                }
            }
            return -1;
        }

        public void Clear() {
            List<T> copy = new List<T>(list);
            foreach (T track in copy) {
                Remove(track);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Sort(IComparer<T> comparer) {
            list.Sort(comparer);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Move(int oldIndex, int newIndex) {
            var item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }

        /// <summary>
        /// Gets the time at which the earliest track in the tracklist starts.
        /// </summary>
        public TimeSpan Start {
            get {
                if (Count == 0) {
                    return TimeSpan.Zero;
                }
                TimeSpan start = TimeSpan.MaxValue;
                foreach (T track in this) {
                    if (track.Offset < start) {
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
                if (Count == 0) {
                    return TimeSpan.Zero;
                }
                TimeSpan end = TimeSpan.MinValue;
                foreach (T track in this) {
                    if (track.Offset + track.Length > end) {
                        end = track.Offset + track.Length;
                    }
                }
                return end;
            }
        }

        /// <summary>
        /// Gets the total length of all tracks added together.
        /// </summary>
        public TimeSpan TotalLength {
            get {
                TimeSpan total = new TimeSpan();
                foreach (T track in this) {
                    total += track.Length;
                }
                return total;
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

        public IEnumerable<T> EnumerateAtPosition(TimeSpan position) {
            foreach (T track in this) {
                if (track.Offset <= position && track.Offset + track.Length >= position) {
                    yield return track;
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        protected void OnTrackListChanged() {
            OnPropertyChanged("TotalLength");
            OnPropertyChanged("Start");
            OnPropertyChanged("End");
            OnPropertyChanged("Count");
        }

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args) {
            if (CollectionChanged != null) {
                CollectionChanged(this, args);
            }
        }

        #endregion
    }
}
