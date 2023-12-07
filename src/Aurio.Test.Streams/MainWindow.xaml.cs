using System.Windows;
using Aurio.Resampler;
using Aurio.Streams;
using NAudio.Wave;

namespace Aurio.Test.Streams
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Use Soxr as resampler implementation
            ResamplerFactory.Factory = new Aurio.Soxr.ResamplerFactory();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true)
            {
                WaveFileReader reader = new WaveFileReader(dlg.FileName);

                NAudioSourceStream nAudioSource = new NAudioSourceStream(reader);

                IeeeStream ieee = new IeeeStream(nAudioSource);

                MonoStream mono = new MonoStream(ieee);
                ResamplingStream res = new ResamplingStream(mono, ResamplingQuality.Medium, 22050);

                NAudioSinkStream nAudioSink = new NAudioSinkStream(res);

                WaveFileWriter.CreateWaveFile(dlg.FileName + ".processed.wav", nAudioSink);
            }
        }
    }
}
