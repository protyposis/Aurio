using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AudioAlign.Audio.Project;
using System.IO;
using NAudio.Wave;
using AudioAlign.Audio.Streams;
using System.Timers;
using System.Windows.Interop;
using System.Windows.Threading;
using AudioAlign.Audio;

namespace AudioAlign.Test.MultitrackPlayback {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private const ulong SEEKER_PROGRAMMATIC_VALUECHANGED_TAG = 0xDEADC0DEDEADC0DE;

        private Timer timer;
        private WaveOut wavePlayer;
        private IAudioStream playbackStream;
        private DebugStreamController debugStreamController;

        public MainWindow() {
            InitializeComponent();
        }

        private void btnAddFile_Click(object sender, RoutedEventArgs e) {
            // http://msdn.microsoft.com/en-us/library/aa969773.aspx
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "audio"; // Default file name
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "Wave files (.wav)|*.wav"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true) {
                // Open document
                AddFile(dlg.FileName);
            }
        }

        private void AddFile(string fileName) {
            if (AudioStreamFactory.IsSupportedFile(fileName)) {
                AudioTrack audioTrack = new AudioTrack(new FileInfo(fileName));
                trackListBox.Items.Add(audioTrack);
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e) {
            if (wavePlayer != null) {
                wavePlayer.Dispose();
            }
            debugStreamController = new DebugStreamController();

            MixerStream mixer = new MixerStream(2, 44100);
            foreach (AudioTrack audioTrack in trackListBox.Items) {
                WaveFileReader reader = new WaveFileReader(audioTrack.FileInfo.FullName);
                IeeeStream channel = new IeeeStream(new DebugStream(new NAudioSourceStream(reader), debugStreamController));
                //ResamplingStream res = new ResamplingStream(new DebugStream(channel, debugStreamController), ResamplingQuality.SincBest, 22050);

                TimeWarpStream warp = new TimeWarpStream(new DebugStream(channel, debugStreamController), ResamplingQuality.SincBest);
                warp.AddMapping(new Mapping { From = channel.Length / 10 * 4, To = channel.Length / 9 });
                warp.AddMapping(new Mapping { From = channel.Length / 10 * 5, To = channel.Length / 9 * 2 });
                warp.AddMapping(new Mapping { From = channel.Length / 10 * 10, To = channel.Length / 9 * 3 });

                // necessary to control each track individually
                VolumeControlStream volumeControl = new VolumeControlStream(new DebugStream(warp, debugStreamController)) {
                    Mute = audioTrack.Mute,
                    Volume = audioTrack.Volume
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

                        foreach (AudioTrack vaudioTrack in trackListBox.Items) {
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
                            foreach (AudioTrack vaudioTrack in trackListBox.Items) {
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

                mixer.Add(new DebugStream(volumeControl));
            }

            VolumeControlStream volumeControlStream = new VolumeControlStream(new DebugStream(mixer, debugStreamController)) {
                Volume = (float)volumeSlider.Value
            };
            VolumeMeteringStream volumeMeteringStream = new VolumeMeteringStream(new DebugStream(volumeControlStream, debugStreamController), 5000);
            volumeMeteringStream.StreamVolume += new EventHandler<StreamVolumeEventArgs>(meteringStream_StreamVolume);
            VolumeClipStream volumeClipStream = new VolumeClipStream(new DebugStream(volumeMeteringStream, debugStreamController));

            playbackStream = volumeClipStream;

            wavePlayer = new WaveOut();
            wavePlayer.DesiredLatency = 250;
            wavePlayer.Init(new NAudioSinkStream(new DebugStream(playbackStream, debugStreamController)));

            // master volume setting
            volumeSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(
                delegate(object vsender, RoutedPropertyChangedEventArgs<double> ve) {
                    volumeControlStream.Volume = (float)ve.NewValue;
            });

            lblTotalPlaybackTime.Content = TimeUtil.BytesToTimeSpan(playbackStream.Length, playbackStream.Properties);
            playbackSeeker.Maximum = TimeUtil.BytesToTimeSpan(playbackStream.Length, playbackStream.Properties).TotalSeconds;

            wavePlayer.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e) {
            if (wavePlayer == null)
                return;

            if (wavePlayer.PlaybackState == PlaybackState.Paused) {
                wavePlayer.Play();
            }
            else {
                wavePlayer.Pause();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e) {
            if (wavePlayer == null)
                return;

            wavePlayer.Stop();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            timer = new Timer(50);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = true;
        }

        private void Window_Closed(object sender, EventArgs e) {
            if (playbackStream != null) {
                //playbackStream.Close();
            }

            if (wavePlayer != null) {
                wavePlayer.Dispose();
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (wavePlayer != null) {
                lblCurrentPlaybackTime.Dispatcher.BeginInvoke(DispatcherPriority.Normal, 
                    new DispatcherOperationCallback(delegate {
                        lblCurrentPlaybackTime.Content = TimeUtil.BytesToTimeSpan(playbackStream.Position, playbackStream.Properties);

                        playbackSeeker.Tag = SEEKER_PROGRAMMATIC_VALUECHANGED_TAG;
                        playbackSeeker.Value = TimeUtil.BytesToTimeSpan(playbackStream.Position, playbackStream.Properties).TotalSeconds;

                        return null;
                   }), null);
            }
        }

        private void meteringStream_StreamVolume(object sender, StreamVolumeEventArgs e) {
            if (e.MaxSampleValues.Length >= 2) {
                stereoVUMeter1.AmplitudeLeft = e.MaxSampleValues[0];
                stereoVUMeter1.AmplitudeRight = e.MaxSampleValues[1];
            }
        }

        private void playbackSeeker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (playbackSeeker.Tag != null && (ulong)playbackSeeker.Tag == SEEKER_PROGRAMMATIC_VALUECHANGED_TAG) {
                playbackSeeker.Tag = null;
                return;
            }

            if (playbackStream != null) {
                debugStreamController.PrintPositions("before");
                playbackStream.Position = Math.Min(
                    TimeUtil.TimeSpanToBytes(TimeSpan.FromSeconds(playbackSeeker.Value), playbackStream.Properties), 
                    playbackStream.Length);
                debugStreamController.PrintPositions("after");
            }
        }

        private void trackListBox_Drop(object sender, DragEventArgs e) {
            // source: http://stackoverflow.com/questions/332859/detect-dragndrop-file-in-wpf
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList()) {
                foreach (string filePath in ((DataObject)e.Data).GetFileDropList()) {
                    AddFile(filePath);
                }
            }
        }
    }
}
