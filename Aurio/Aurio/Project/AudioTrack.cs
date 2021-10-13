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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Aurio.Streams;
using System.Security.Cryptography;

namespace Aurio.Project {
    public class AudioTrack : Track {

        public const string PEAKFILE_EXTENSION = ".aapeaks";

        public event EventHandler<ValueEventArgs<bool>> MuteChanged;
        public event EventHandler<ValueEventArgs<bool>> SoloChanged;
        public event EventHandler<ValueEventArgs<float>> VolumeChanged;
        public event EventHandler<ValueEventArgs<float>> BalanceChanged;
        public event EventHandler<ValueEventArgs<bool>> InvertedPhaseChanged;
        public event EventHandler<ValueEventArgs<bool>> MonoDownmixChanged;

        private AudioProperties sourceProperties;
        private bool mute = false;
        private bool solo = false;
        private float volume = 1.0f;
        private float balance = 0.0f;
        private bool invertedPhase = false;
        private bool monoDownmix = false;
        private TimeWarpCollection timeWarps;

        public AudioTrack(FileInfo[] fileInfos, bool initialize, FileInfo[] proxyFileInfos = null)
            : base(fileInfos) {
                this.TimeWarps = new TimeWarpCollection();
                ProxyFileInfos = proxyFileInfos;
                if (initialize) {
                    using (IAudioStream stream = AudioStreamFactory.FromFileInfo(FileInfo, ProxyFileInfos.Length > 0 ? ProxyFileInfo : null)) {
                        sourceProperties = stream.Properties;
                        if (MultiFile) {
                            // For multi-file tracks, we need to get a concatenated stream of all files for the length
                            InitializeLength();
                        }
                        else {
                            // Single-file tracks can just reuse this stream to get the length
                            InitializeLength(stream);
                        }
                    }
            }
        }

        public AudioTrack(FileInfo fileInfo, bool initialize)
            : this(new FileInfo[] { fileInfo }, initialize) {
        }

        public AudioTrack(FileInfo[] fileInfos)
            : this(fileInfos, true) {
        }

        public AudioTrack(FileInfo fileInfo)
            : this(new FileInfo[] { fileInfo }, true) {
        }

        public AudioTrack(FileInfo fileInfo, FileInfo proxyFileInfo)
            : this(new FileInfo[] { fileInfo }, true, new FileInfo[] { proxyFileInfo })
        {
        }

        public AudioTrack(IAudioStream stream, string name) : base(stream, name) {
            sourceProperties = stream.Properties;
        }

        public override MediaType MediaType {
            get { return MediaType.Audio; }
        }

        private void InitializeLength(IAudioStream audioStream = null) {
            using (audioStream = audioStream ?? CreateAudioStream()) {
                Length = TimeUtil.BytesToTimeSpan(audioStream.Length, audioStream.Properties);
            }
        }

        public IAudioStream CreateAudioStream(bool warp = true) {
            IAudioStream stream = null;
            if(MultiFile) {
                var fileInfos = ProxyFileInfos.Select(fi => fi.Exists).Count() < ProxyFileInfos.Length ? FileInfos : ProxyFileInfos;
                stream = new ConcatenationStream(fileInfos.Select(fi => AudioStreamFactory.FromFileInfoIeee32(fi)).ToArray());
            } else {
                if (HasProxyFile)
                {
                    stream = AudioStreamFactory.FromFileInfoIeee32(ProxyFileInfo);
                }
                else
                {
                    stream = AudioStreamFactory.FromFileInfoIeee32(FileInfo, ProxyFileInfo);
                }
            }

            if (warp)
            {
                return new TimeWarpStream(stream, timeWarps);
            }

            return stream;
        }

        protected string GeneratePeakFileName() {
            string name = GenerateName();
            if(MultiFile) {
                // When the track consists of multiple files, we take the default track name and append a hash
                // value calculated from all files belonging to this track. This results in a short name while
                // avoiding file name collisions when different sets of concatenated files start with the same file (name).
                string concatenatedNames = FileInfos.Select(fi => fi.Name).Aggregate((s1, s2) => s1 + "+" + s2);
                using (var sha = SHA1.Create()) {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(concatenatedNames));
                    string hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();

                    // append Git-style shorted hash
                    name += "[" + hashString.Substring(0, 6) + "]";
                }
            }
            return name;
        }

        public FileInfo PeakFile {
            get {
                return new FileInfo(Path.Combine(FileInfo.DirectoryName, GeneratePeakFileName() + PEAKFILE_EXTENSION));
            }
        }

        public bool HasPeakFile {
            get {
                return PeakFile.Exists;
            }
        }

        public AudioProperties SourceProperties {
            get { return sourceProperties; }
        }

        public FileInfo ProxyFileInfo
        {
            get { return ProxyFileInfos.Length > 0 ? ProxyFileInfos[0] : null; }
        }

        public FileInfo[] ProxyFileInfos { get; private set; }

        public bool HasProxyFile
        {
            get
            {
                return ProxyFileInfo.Exists;
            }
        }

        /// <summary>
        /// Gets or sets a value telling is this track is muted.
        /// </summary>
        public bool Mute { get { return mute; } set { mute = value; OnMuteChanged(); } }

        /// <summary>
        /// Gets or sets a value that tells is this track is to be played solo.
        /// If the solo property of at least one track in a project is set to true, only the tracks with
        /// solo set to true will be played.
        /// </summary>
        public bool Solo { get { return solo; } set { solo = value; OnSoloChanged(); } }

        /// <summary>
        /// Gets or sets the volume of this track. 0.0f equals to mute, 1.0f is the default audio 
        /// level (volume stays unchanged). 2.0f means the volume will be twice the default intensity.
        /// </summary>
        public float Volume { get { return volume; } set { volume = value; OnVolumeChanged(); } }

        /// <summary>
        /// Gets or sets the panning of this track.
        /// </summary>
        public float Balance { get { return balance; } set { balance = value; OnBalanceChanged(); } }

        /// <summary>
        /// Gets or sets a value telling if this track's audio phase is inverted.
        /// </summary>
        public bool InvertedPhase { get { return invertedPhase; } set { invertedPhase = value; OnInvertedPhaseChanged(); } }

        /// <summary>
        /// Gets or sets a value telling if this track's audio channels should be downmixed to a mono signal.
        /// </summary>
        public bool MonoDownmix { get { return monoDownmix; } set { monoDownmix = value; OnMonoDownmixChanged(); } }

        public TimeWarpCollection TimeWarps {
            get { return timeWarps; }
            set { 
                timeWarps = value;
                timeWarps.CollectionChanged += timeWarps_CollectionChanged;
            }
        }

        private void timeWarps_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null || e.OldItems != null || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) {
                InitializeLength();
            }
        }

        private void OnMuteChanged() {
            if (MuteChanged != null) {
                MuteChanged(this, new ValueEventArgs<bool>(mute));
            }
            OnPropertyChanged("Mute");
        }

        private void OnSoloChanged() {
            if (solo) {
                Mute = false;
            }

            if (SoloChanged != null) {
                SoloChanged(this, new ValueEventArgs<bool>(solo));
            }
            OnPropertyChanged("Solo");
        }

        private void OnVolumeChanged() {
            if (VolumeChanged != null) {
                VolumeChanged(this, new ValueEventArgs<float>(volume));
            }
            OnPropertyChanged("Volume");
        }

        private void OnBalanceChanged() {
            if (BalanceChanged != null) {
                BalanceChanged(this, new ValueEventArgs<float>(balance));
            }
            OnPropertyChanged("Balance");
        }

        private void OnInvertedPhaseChanged() {
            if (InvertedPhaseChanged != null) {
                InvertedPhaseChanged(this, new ValueEventArgs<bool>(invertedPhase));
            }
            OnPropertyChanged("InvertedPhase");
        }

        private void OnMonoDownmixChanged() {
            if (MonoDownmixChanged != null) {
                MonoDownmixChanged(this, new ValueEventArgs<bool>(monoDownmix));
            }
            OnPropertyChanged("MonoDownmix");
        }

        public override string ToString() {
            return "Audio" + base.ToString();
        }
    }
}
