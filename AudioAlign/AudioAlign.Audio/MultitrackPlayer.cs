using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.NAudio;
using AudioAlign.Audio.Project;
using NAudio.Wave;
using System.Timers;
using System.Diagnostics;

namespace AudioAlign.Audio {
    public class MultitrackPlayer : IDisposable {

        public event EventHandler PlaybackStateChanged;
        public event EventHandler PlaybackStarted;
        public event EventHandler PlaybackPaused;
        public event EventHandler PlaybackStopped;

        public event EventHandler<StreamVolumeEventArgs> VolumeAnnounced;
        public event EventHandler<ValueEventArgs<TimeSpan>> CurrentTimeChanged;

        private TrackList<AudioTrack> trackList;
        private Dictionary<AudioTrack, WaveStream> trackListStreams;

        private ExtendedWaveMixerStream32 audioMixer;
        private VolumeControlStream audioVolumeControlStream;
        private WaveStream audioOutputStream;
        private IWavePlayer audioOutput;

        private Timer timer;

        public MultitrackPlayer(TrackList<AudioTrack> trackList) {
            this.trackList = trackList;
            trackListStreams = new Dictionary<AudioTrack, WaveStream>();

            trackList.TrackAdded += new TrackList<AudioTrack>.TrackListChangedEventHandler(trackList_TrackAdded);
            trackList.TrackRemoved += new TrackList<AudioTrack>.TrackListChangedEventHandler(trackList_TrackRemoved);

            SetupAudioChain();

            foreach (AudioTrack audioTrack in trackList) {
                AddTrack(audioTrack);
            }

            timer = new Timer(50);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }

        public float Volume {
            get { return audioVolumeControlStream.Volume; }
            set { audioVolumeControlStream.Volume = value; }
        }

        public TimeSpan TotalTime {
            get { return audioOutputStream.TotalTime; }
        }

        public TimeSpan CurrentTime {
            get { return audioOutputStream.CurrentTime; }
            set { audioOutputStream.CurrentTime = value; }
        }

        public bool CanPlay {
            get { return audioOutput.PlaybackState != PlaybackState.Playing && trackList.Count > 0; }
        }

        public bool CanPause {
            get { return audioOutput.PlaybackState == PlaybackState.Playing; }
        }

        public bool Play() {
            if (audioOutput.PlaybackState != PlaybackState.Playing) {
                if (audioOutput.PlaybackState == PlaybackState.Stopped) {
                    audioOutput.Play();
                }
                else if (audioOutput.PlaybackState == PlaybackState.Paused) {
                    audioOutput.Play();
                }
                timer.Enabled = true;
                OnPlaybackStarted();
                return true;
            }
            return false;
        }

        public bool Pause() {
            audioOutput.Stop();
            timer.Enabled = false;
            OnPlaybackPaused();
            OnVolumeAnnounced(new StreamVolumeEventArgs() { 
                MaxSampleValues = new float[] { float.NegativeInfinity, float.NegativeInfinity } 
            });
            return true;
        }

        private void SetupAudioChain() {
            audioMixer = new ExtendedWaveMixerStream32();

            audioVolumeControlStream = new VolumeControlStream(audioMixer);
            VolumeMeteringStream volumeMeteringStream = new VolumeMeteringStream(audioVolumeControlStream);
            volumeMeteringStream.StreamVolume += new EventHandler<StreamVolumeEventArgs>(meteringStream_StreamVolume);
            VolumeClipStream volumeClipStream = new VolumeClipStream(volumeMeteringStream);

            audioOutputStream = volumeClipStream;

            audioOutput = new WasapiOut(global::NAudio.CoreAudioApi.AudioClientShareMode.Shared, true, 10);
            audioOutput.PlaybackStopped += new EventHandler(
                delegate(object sender, EventArgs e) {
                    Pause();
                    OnPlaybackPaused();
                });
            audioOutput.Init(audioOutputStream);
        }

        private void AddTrack(AudioTrack audioTrack) {
            WaveFileReader reader = new WaveFileReader(audioTrack.FileInfo.FullName);
            TolerantWaveStream tolerantReader = new TolerantWaveStream(reader);
            WaveOffsetStream offsetStream = new WaveOffsetStream(tolerantReader);
            ExtendedWaveChannel32 channel = new ExtendedWaveChannel32(offsetStream);

            audioTrack.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(
                delegate(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
                    if (e.PropertyName.Equals("Offset")) {
                        offsetStream.StartTime = audioTrack.Offset;
                        channel.UpdateLength();
                        audioMixer.UpdateLength();
                    }
                });

            // control the track phase
            PhaseInversionStream phaseInversion = new PhaseInversionStream(channel) {
                Invert = audioTrack.InvertedPhase
            };

            // necessary to control each track individually
            VolumeControlStream volumeControl = new VolumeControlStream(phaseInversion) {
                Mute = audioTrack.Mute,
                Volume = audioTrack.Volume,
                Balance = audioTrack.Balance
            };

            // when the AudioTrack.Mute property changes, just set it accordingly on the audio stream
            audioTrack.MuteChanged += new EventHandler<ValueEventArgs<bool>>(
                delegate(object vsender, ValueEventArgs<bool> ve) {
                    volumeControl.Mute = ve.Value;
                });

            // when the AudioTrack.Solo property changes, we have to react in different ways:
            audioTrack.SoloChanged += new EventHandler<ValueEventArgs<bool>>(
                delegate(object vsender, ValueEventArgs<bool> ve) {
                    AudioTrack senderTrack = (AudioTrack)vsender;
                    bool isOtherTrackSoloed = false;

                    foreach (AudioTrack vaudioTrack in trackList) {
                        if (vaudioTrack != senderTrack && vaudioTrack.Solo) {
                            isOtherTrackSoloed = true;
                            break;
                        }
                    }

                    /* if there's at least one other track that is soloed, we set the mute property of 
                     * the current track to the opposite of the solo property:
                     * - if the track is soloed, we unmute it
                     * - if the track is unsoloed, we mute it
                     */
                    if (isOtherTrackSoloed) {
                        senderTrack.Mute = !ve.Value;
                    }
                    /* if this is the only soloed track, we mute all other tracks
                     * if this track just got unsoloed, we unmute all other tracks
                     */
                    else {
                        foreach (AudioTrack vaudioTrack in trackList) {
                            if (vaudioTrack != senderTrack && !vaudioTrack.Solo) {
                                vaudioTrack.Mute = ve.Value;
                            }
                        }
                    }
                });

            // when the AudioTrack.Volume property changes, just set it accordingly on the audio stream
            audioTrack.VolumeChanged += new EventHandler<ValueEventArgs<float>>(
                delegate(object vsender, ValueEventArgs<float> ve) {
                    volumeControl.Volume = ve.Value;
                });

            audioTrack.BalanceChanged += new EventHandler<ValueEventArgs<float>>(
                delegate(object vsender, ValueEventArgs<float> ve) {
                    volumeControl.Balance = ve.Value;
                });

            audioTrack.InvertedPhaseChanged += new EventHandler<ValueEventArgs<bool>>(
                delegate(object vsender, ValueEventArgs<bool> ve) {
                    phaseInversion.Invert = ve.Value;
                });

            WaveStream trackStream = volumeControl;

            audioMixer.AddInputStream(trackStream);
            trackListStreams.Add(audioTrack, trackStream);
        }

        private void RemoveTrack(AudioTrack audioTrack) {
            audioMixer.RemoveInputStream(trackListStreams[audioTrack]);
            trackListStreams.Remove(audioTrack);
        }

        private void trackList_TrackAdded(object sender, TrackList<AudioTrack>.TrackListEventArgs e) {
            AddTrack(e.Track);
        }

        private void trackList_TrackRemoved(object sender, TrackList<AudioTrack>.TrackListEventArgs e) {
            RemoveTrack(e.Track);
        }

        private void meteringStream_StreamVolume(object sender, StreamVolumeEventArgs e) {
            OnVolumeAnnounced(e);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e) {
            OnCurrentTimeChanged();
        }

        #region Event firing

        protected virtual void OnPlaybackStateChanged() {
            if (PlaybackStateChanged != null) {
                PlaybackStateChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnPlaybackStarted() {
            if (PlaybackStarted != null) {
                PlaybackStarted(this, EventArgs.Empty);
            }
            OnPlaybackStateChanged();
        }

        protected virtual void OnPlaybackPaused() {
            if (PlaybackPaused != null) {
                PlaybackPaused(this, EventArgs.Empty);
            }
            OnPlaybackStateChanged();
        }

        protected virtual void OnPlaybackStopped() {
            if (PlaybackStopped != null) {
                PlaybackStopped(this, EventArgs.Empty);
            }
            OnPlaybackStateChanged();
        }

        protected virtual void OnVolumeAnnounced(StreamVolumeEventArgs e) {
            if (VolumeAnnounced != null) {
                VolumeAnnounced(this, e);
            }
        }

        protected virtual void OnCurrentTimeChanged() {
            if (CurrentTimeChanged != null) {
                CurrentTimeChanged(this, new ValueEventArgs<TimeSpan>(audioOutputStream.CurrentTime));
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            audioOutputStream.Dispose();
            audioOutput.Dispose();
        }

        #endregion
    }
}
