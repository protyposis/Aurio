using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using NAudio.Wave;
using System.Timers;
using System.Diagnostics;
using AudioAlign.Audio.Streams;
using NAudio.CoreAudioApi;
using AudioAlign.Audio.TaskMonitor;

namespace AudioAlign.Audio {
    public class MultitrackPlayer : IDisposable {

        public event EventHandler PlaybackStateChanged;
        public event EventHandler PlaybackStarted;
        public event EventHandler PlaybackPaused;
        public event EventHandler PlaybackStopped;

        public event EventHandler<StreamVolumeEventArgs> VolumeAnnounced;
        public event EventHandler<ValueEventArgs<TimeSpan>> CurrentTimeChanged;
        public event EventHandler<StreamDataMonitorEventArgs> SamplesMonitored;

        private TrackList<AudioTrack> trackList;
        private Dictionary<AudioTrack, IAudioStream> trackListStreams;

        private MixerStream audioMixer;
        private VolumeControlStream audioVolumeControlStream;
        private VolumeMeteringStream audioVolumeMeteringStream;
        private DataMonitorStream dataMonitorStream;
        private IAudioStream audioOutputStream;
        private IWavePlayer audioOutput;

        private Timer timer;

        public MultitrackPlayer(TrackList<AudioTrack> trackList) {
            this.trackList = trackList;
            trackListStreams = new Dictionary<AudioTrack, IAudioStream>();

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
            get { return TimeUtil.BytesToTimeSpan(audioOutputStream.Length, audioOutputStream.Properties); }
        }

        public TimeSpan CurrentTime {
            get { return TimeUtil.BytesToTimeSpan(audioOutputStream.Position, audioOutputStream.Properties); }
            set { audioOutputStream.Position = TimeUtil.TimeSpanToBytes(value, audioOutputStream.Properties); }
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

        public void SaveToFile(System.IO.FileInfo outputFile, IProgressReporter progressReporter) {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // save current position for later
            var currentTime = CurrentTime;
            // disable reporting to consumers (e.g. GUI)
            audioVolumeMeteringStream.Disabled = dataMonitorStream.Disabled = true;

            // set to start for all audio to be saved
            audioOutputStream.Position = 0;

            NAudioSinkStream nAudioSink = new NAudioSinkStream(audioOutputStream);
            long total = nAudioSink.Length;
            long progress = 0;
            using (WaveFileWriter writer = new WaveFileWriter(outputFile.FullName, nAudioSink.WaveFormat)) {
                byte[] buffer = new byte[nAudioSink.WaveFormat.AverageBytesPerSecond * 5];
                while (true) {
                    int bytesRead = nAudioSink.Read(buffer, 0, buffer.Length);
                    progress += bytesRead;
                    if (bytesRead == 0) {
                        // end of stream reached
                        break;
                    }
                    writer.Write(buffer, 0, bytesRead);

                    if (progressReporter != null) {
                        progressReporter.ReportProgress((double)progress / total * 100);
                    }
                }
            }

            // reenable reporting to consumers
            audioVolumeMeteringStream.Disabled = dataMonitorStream.Disabled = false;
            // reset initial position
            CurrentTime = currentTime;

            sw.Stop();
            Console.WriteLine("Export time: " + sw.Elapsed);
        }

        public void SaveToFile(System.IO.FileInfo outputFile) {
            SaveToFile(outputFile, null);
        }

        private void SetupAudioChain() {
            /* Obtain output device to read the rendering samplerate and initialize the mixer stream
             * with the target samplerate to avoid NAudio's internal ResamplerDmoStream for the target
             * samplerate conversion. ResamplerDmoStream works strangely and often requests zero bytes
             * from the audio pipeline which makes the playback stop and screws up the whole playback
             * in AudioAlign. */
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice mmdevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            Console.WriteLine("audio playback endpoint: " + mmdevice.FriendlyName);
            Console.WriteLine("format: " + mmdevice.AudioClient.MixFormat);

            // init mixer stream with device playback samplerate
            audioMixer = new MixerStream(2, mmdevice.AudioClient.MixFormat.SampleRate);

            audioVolumeControlStream = new VolumeControlStream(audioMixer);
            audioVolumeMeteringStream = new VolumeMeteringStream(audioVolumeControlStream);
            dataMonitorStream = new DataMonitorStream(audioVolumeMeteringStream);
            dataMonitorStream.DataRead += new EventHandler<StreamDataMonitorEventArgs>(dataMonitorStream_DataRead);
            VolumeClipStream volumeClipStream = new VolumeClipStream(dataMonitorStream);

            audioOutputStream = volumeClipStream;

            audioOutput = new WasapiOut(global::NAudio.CoreAudioApi.AudioClientShareMode.Shared, true, 200);
            audioOutput.PlaybackStopped += new EventHandler<StoppedEventArgs>(
                delegate(object sender, StoppedEventArgs e) {
                    OnCurrentTimeChanged();
                    Pause();
                    OnPlaybackPaused();
                });
            audioOutput.Init(new NAudioSinkStream(audioOutputStream));
        }

        private void AddTrack(AudioTrack audioTrack) {
            IAudioStream input = AudioStreamFactory.FromFileInfo(audioTrack.FileInfo);
            IAudioStream baseStream = new ResamplingStream( // adjust sample rate to mixer output rate
                new IeeeStream(new TolerantStream(new BufferedStream(input, 1024 * 1024, true))), 
                ResamplingQuality.SincFastest, audioMixer.Properties.SampleRate);
            TimeWarpStream timeWarpStream = new TimeWarpStream(baseStream, ResamplingQuality.SincFastest) {
                Mappings = audioTrack.TimeWarps
            };
            OffsetStream offsetStream = new OffsetStream(timeWarpStream) {
                Offset = TimeUtil.TimeSpanToBytes(audioTrack.Offset, baseStream.Properties) 
            };

            audioTrack.OffsetChanged += new EventHandler<ValueEventArgs<TimeSpan>>(
                delegate(object sender, ValueEventArgs<TimeSpan> e) {
                    offsetStream.Offset = TimeUtil.TimeSpanToBytes(e.Value, offsetStream.Properties);
                    audioMixer.UpdateLength();
                });

            // control the track phase
            PhaseInversionStream phaseInversion = new PhaseInversionStream(offsetStream) {
                Invert = audioTrack.InvertedPhase
            };

            MonoStream monoStream = new MonoStream(phaseInversion, phaseInversion.Properties.Channels) {
                Downmix = audioTrack.MonoDownmix
            };

            // necessary to control each track individually
            VolumeControlStream volumeControl = new VolumeControlStream(monoStream) {
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
            audioTrack.MonoDownmixChanged += new EventHandler<ValueEventArgs<bool>>(
                delegate(object vsender, ValueEventArgs<bool> ve) {
                    monoStream.Downmix = ve.Value;
                });

            IAudioStream trackStream = volumeControl;

            if (trackStream.Properties.Channels == 1 && audioMixer.Properties.Channels > 1) {
                trackStream = new MonoStream(trackStream, audioMixer.Properties.Channels);
            }

            audioMixer.Add(trackStream);
            trackListStreams.Add(audioTrack, trackStream);
        }

        private void RemoveTrack(AudioTrack audioTrack) {
            audioMixer.Remove(trackListStreams[audioTrack]);
            trackListStreams.Remove(audioTrack);
        }

        private void trackList_TrackAdded(object sender, TrackList<AudioTrack>.TrackListEventArgs e) {
            AddTrack(e.Track);
        }

        private void trackList_TrackRemoved(object sender, TrackList<AudioTrack>.TrackListEventArgs e) {
            RemoveTrack(e.Track);
        }

        private void dataMonitorStream_DataRead(object sender, StreamDataMonitorEventArgs e) {
            OnSamplesMonitored(e);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e) {
            OnCurrentTimeChanged();
            OnVolumeAnnounced(new StreamVolumeEventArgs { MaxSampleValues = audioVolumeMeteringStream.GetMaxSampleValues() });
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
                CurrentTimeChanged(this, new ValueEventArgs<TimeSpan>(CurrentTime));
            }
        }

        private void OnSamplesMonitored(StreamDataMonitorEventArgs e) {
            if (SamplesMonitored != null) {
                SamplesMonitored(this, e);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            audioOutput.Dispose();
        }

        #endregion
    }
}
