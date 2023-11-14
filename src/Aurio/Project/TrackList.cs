//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Aurio.Project
{
    public class TrackList<T> : IEnumerable<T>, INotifyPropertyChanged, INotifyCollectionChanged
        where T : Track
    {
        private readonly List<T> list;

        public class TrackListEventArgs : EventArgs
        {
            public TrackListEventArgs(T track, int index)
            {
                this.Track = track;
                this.Index = index;
            }

            public T Track { get; private set; }
            public int Index { get; private set; }
        }

        public delegate void TrackListChangedEventHandler(object sender, TrackListEventArgs e);
        public event TrackListChangedEventHandler TrackAdded;
        public event TrackListChangedEventHandler TrackRemoved;

        public TrackList()
        {
            list = new List<T>();
        }

        public TrackList(IEnumerable<T> collection)
        {
            list = new List<T>(collection);
        }

        private void OnTrackAdded(TrackListEventArgs e)
        {
            if (TrackAdded != null)
            {
                TrackAdded(this, e);
            }
            OnTrackListChanged();
        }

        private void OnTrackRemoved(TrackListEventArgs e)
        {
            if (TrackRemoved != null)
            {
                TrackRemoved(this, e);
            }
            OnTrackListChanged();
        }

        public void Add(T track)
        {
            list.Add(track);
            OnTrackAdded(new TrackListEventArgs(track, list.IndexOf(track)));
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    track,
                    list.IndexOf(track)
                )
            );
            track.LengthChanged += Track_LengthOrOffsetChanged;
            track.OffsetChanged += Track_LengthOrOffsetChanged;
        }

        public void Add(IEnumerable<T> tracks)
        {
            int startIndex = list.Count;
            foreach (T track in tracks)
            {
                list.Add(track);
                OnTrackAdded(new TrackListEventArgs(track, list.IndexOf(track)));
            }
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    new List<T>(tracks),
                    startIndex
                )
            );
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public int Count
        {
            get { return list.Count; }
        }

        private bool Remove(T track, bool suppressCollectionChangedEvent)
        {
            if (list.Contains(track))
            {
                int index = list.IndexOf(track);
                if (list.Remove(track))
                {
                    track.LengthChanged -= Track_LengthOrOffsetChanged;
                    track.OffsetChanged -= Track_LengthOrOffsetChanged;
                    OnTrackRemoved(new TrackListEventArgs(track, index));
                    if (!suppressCollectionChangedEvent)
                    {
                        OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Remove,
                                track,
                                index
                            )
                        );
                    }
                    return true;
                }
            }
            return false;
        }

        public bool Remove(T track)
        {
            return Remove(track, false);
        }

        public T this[int index]
        {
            get { return list[index]; }
        }

        public int IndexOf(T element)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == element)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Clear()
        {
            List<T> copy = new List<T>(list);
            foreach (T track in copy)
            {
                Remove(track, true);
            }
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
            );
        }

        public void Sort(IComparer<T> comparer)
        {
            list.Sort(comparer);
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
            );
        }

        public void Move(int oldIndex, int newIndex)
        {
            var item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Move,
                    item,
                    newIndex,
                    oldIndex
                )
            );
        }

        /// <summary>
        /// Gets the time at which the earliest track in the tracklist starts.
        /// </summary>
        public TimeSpan Start
        {
            get
            {
                if (Count == 0)
                {
                    return TimeSpan.Zero;
                }
                TimeSpan start = TimeSpan.MaxValue;
                foreach (T track in this)
                {
                    if (track.Offset < start)
                    {
                        start = track.Offset;
                    }
                }
                return start;
            }
        }

        /// <summary>
        /// Gets the time at which the latest track in the tracklist ends.
        /// </summary>
        public TimeSpan End
        {
            get
            {
                if (Count == 0)
                {
                    return TimeSpan.Zero;
                }
                TimeSpan end = TimeSpan.MinValue;
                foreach (T track in this)
                {
                    if (track.Offset + track.Length > end)
                    {
                        end = track.Offset + track.Length;
                    }
                }
                return end;
            }
        }

        /// <summary>
        /// Gets the total length of all tracks added together.
        /// </summary>
        public TimeSpan TotalLength
        {
            get
            {
                TimeSpan total = new TimeSpan();
                foreach (T track in this)
                {
                    total += track.Length;
                }
                return total;
            }
        }

        private void Track_LengthOrOffsetChanged(object sender, ValueEventArgs<TimeSpan> e)
        {
            // if a track length or offset changes, it might affect the whole tracklist
            // TODO check if tracklist properties are affected and only fire event in these cases
            OnTrackListChanged();
        }

        #region IEnumerable<T> Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        public IEnumerable<T> EnumerateAtPosition(TimeSpan position)
        {
            foreach (T track in this)
            {
                if (track.Offset <= position && track.Offset + track.Length >= position)
                {
                    yield return track;
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        protected void OnTrackListChanged()
        {
            OnPropertyChanged("TotalLength");
            OnPropertyChanged("Start");
            OnPropertyChanged("End");
            OnPropertyChanged("Count");
        }

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, args);
            }
        }

        #endregion
    }
}
