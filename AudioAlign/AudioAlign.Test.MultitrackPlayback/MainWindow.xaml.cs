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
using AudioAlign.Audio.NAudio;
using System.Timers;
using System.Windows.Interop;
using System.Windows.Threading;

namespace AudioAlign.Test.MultitrackPlayback {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private Timer timer;
        private IWavePlayer wavePlayer;
        private WaveStream playbackStream;

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
                AudioTrack audioTrack = new AudioTrack(new FileInfo(dlg.FileName));
                //audioTrack.Offset = new TimeSpan(new Random().Next((int)new TimeSpan(0, 10, 0).Ticks));
                trackListBox.Items.Add(audioTrack);
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e) {
            if (wavePlayer != null) {
                wavePlayer.Dispose();
            }

            WaveMixerStream32 mixer = new WaveMixerStream32();
            foreach (AudioTrack audioTrack in trackListBox.Items) {
                WaveFileReader reader = new WaveFileReader(audioTrack.FileInfo.FullName);
                WaveChannel32 channel = new WaveChannel32(reader);
                mixer.AddInputStream(channel);
            }

            VolumeControlStream volumeControlStream = new VolumeControlStream(mixer);
            VolumeMeteringStream volumeMeteringStream = new VolumeMeteringStream(volumeControlStream);
            volumeMeteringStream.StreamVolume += new EventHandler<StreamVolumeEventArgs>(meteringStream_StreamVolume);

            playbackStream = volumeMeteringStream;

            wavePlayer = new WaveOut();
            wavePlayer.Init(playbackStream);

            volumeSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(
                delegate(object vsender, RoutedPropertyChangedEventArgs<double> ve) {
                    volumeControlStream.Volume = (float)ve.NewValue;
            });

            wavePlayer.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e) {
            if (wavePlayer.PlaybackState == PlaybackState.Paused) {
                wavePlayer.Play();
            }
            else {
                wavePlayer.Pause();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e) {
            wavePlayer.Stop();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            timer = new Timer(50);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = true;
        }

        private void Window_Closed(object sender, EventArgs e) {
            if (playbackStream != null) {
                playbackStream.Close();
            }

            if (wavePlayer != null) {
                wavePlayer.Dispose();
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (wavePlayer != null) {
                lblCurrentPlaybackTime.Dispatcher.BeginInvoke(DispatcherPriority.Normal, 
                    new DispatcherOperationCallback(delegate {
                        lblCurrentPlaybackTime.Content = playbackStream.CurrentTime.ToString();
                        return null;
                   }), null);
            }
        }

        private void meteringStream_StreamVolume(object sender, StreamVolumeEventArgs e) {
            if (e.MaxSampleValues.Length >= 2) {
                vUMeterCh1.Amplitude = e.MaxSampleValues[0];
                vUMeterCh2.Amplitude = e.MaxSampleValues[1];
            }
        }
    }
}
