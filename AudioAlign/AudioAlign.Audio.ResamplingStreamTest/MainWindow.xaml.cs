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
using NAudio.Wave;
using AudioAlign.Audio.Streams;

namespace AudioAlign.LibSampleRate.Test {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private MixerStream mixer;
        WasapiOut audioOutput;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            audioOutput = new WasapiOut(global::NAudio.CoreAudioApi.AudioClientShareMode.Shared, true, 10);
            mixer = new MixerStream(2, 44100);
            MonoStream mono = new MonoStream(mixer);
            ResamplingStream resampler = new ResamplingStream(mono, ResamplingQuality.SincMedium, 44100);
            NAudioSinkStream naudioSink = new NAudioSinkStream(resampler);
            audioOutput.Init(naudioSink);

            sliderSampleRate.ValueChanged += new RoutedPropertyChangedEventHandler<double>(delegate(object s2, RoutedPropertyChangedEventArgs<double> e2) {
                if (resampler.CheckTargetSampleRate(sliderSampleRate.Value)) {
                    resampler.TargetSampleRate = sliderSampleRate.Value;
                }
            });
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true) {
                mixer.Clear();
                mixer.Add(new IeeeStream(new NAudioSourceStream(new WaveFileReader(dlg.FileName))));
                lblFile.Content = dlg.FileName;
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e) {
            if (audioOutput.PlaybackState == PlaybackState.Playing)
                audioOutput.Pause();
            else if (audioOutput.PlaybackState == PlaybackState.Paused || audioOutput.PlaybackState == PlaybackState.Stopped)
                audioOutput.Play();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            audioOutput.Stop();
        }
    }
}
