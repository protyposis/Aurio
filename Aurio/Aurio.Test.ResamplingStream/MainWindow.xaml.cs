using System.Windows;
using Aurio.Resampler;
using Aurio.Streams;
using NAudio.Wave;

namespace Aurio.Test.ResamplingStream
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private MixerStream mixer;
        WasapiOut audioOutput;

        public MainWindow()
        {
            ResamplerFactory.Factory = new Soxr.ResamplerFactory();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            audioOutput = new WasapiOut(global::NAudio.CoreAudioApi.AudioClientShareMode.Shared, true, 10);
            mixer = new MixerStream(2, 44100);
            MonoStream mono = new MonoStream(mixer);
            Streams.ResamplingStream resampler = new Streams.ResamplingStream(mono, ResamplingQuality.VariableRate, 44100);
            NAudioSinkStream naudioSink = new NAudioSinkStream(resampler);
            audioOutput.Init(naudioSink);

            mixingSampleRateLabel.Content = mixer.Properties.SampleRate;
            playbackSampleRateLabel.Content = audioOutput.OutputWaveFormat.SampleRate;

            sliderSampleRate.ValueChanged += new RoutedPropertyChangedEventHandler<double>(delegate (object s2, RoutedPropertyChangedEventArgs<double> e2)
            {
                if (resampler.CheckTargetSampleRate(sliderSampleRate.Value))
                {
                    resampler.TargetSampleRate = sliderSampleRate.Value;
                }
            });
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true)
            {
                var stream = new IeeeStream(new NAudioSourceStream(new WaveFileReader(dlg.FileName)));
                mixer.Clear();
                mixer.Add(stream);
                lblFile.Content = dlg.FileName;

                fileSampleRateLabel.Content = stream.Properties.SampleRate;
                sliderSampleRate.Value = stream.Properties.SampleRate;
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (audioOutput.PlaybackState == PlaybackState.Playing)
                audioOutput.Pause();
            else if (audioOutput.PlaybackState == PlaybackState.Paused || audioOutput.PlaybackState == PlaybackState.Stopped)
                audioOutput.Play();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            audioOutput.Stop();
        }
    }
}
