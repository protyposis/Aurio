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

namespace AudioAlign.Test.Streams {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true) {
                WaveFileReader reader = new WaveFileReader(dlg.FileName);

                NAudioSourceStream nAudioSource = new NAudioSourceStream(reader);

                IeeeStream ieee = new IeeeStream(nAudioSource);

                MonoStream mono = new MonoStream(ieee);
                ResamplingStream res = new ResamplingStream(mono, ResamplingQuality.SincFastest, 22050);

                NAudioSinkStream nAudioSink = new NAudioSinkStream(res);

                WaveFileWriter.CreateWaveFile(dlg.FileName + ".processed.wav", nAudioSink);
            }
        }
    }
}
