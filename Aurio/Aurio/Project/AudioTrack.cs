using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Aurio.Streams;

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

        public AudioTrack(FileInfo fileInfo, bool initialize)
            : base(fileInfo) {
                this.TimeWarps = new TimeWarpCollection();
                if (initialize) {
                    this.sourceProperties = AudioStreamFactory.FromFileInfo(FileInfo).Properties;
                    InitializeLength();
                }
        }

        public AudioTrack(FileInfo fileInfo)
            : this(fileInfo, true) {
        }

        public override MediaType MediaType {
            get { return MediaType.Audio; }
        }

        private void InitializeLength() {
            IAudioStream audioStream = CreateAudioStream();
            Length = TimeUtil.BytesToTimeSpan(audioStream.Length, audioStream.Properties);
        }

        public IAudioStream CreateAudioStream() {
            return new TimeWarpStream(AudioStreamFactory.FromFileInfoIeee32(FileInfo), timeWarps);
        }

        public FileInfo PeakFile {
            get {
                return new FileInfo(FileInfo.FullName + PEAKFILE_EXTENSION);
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
