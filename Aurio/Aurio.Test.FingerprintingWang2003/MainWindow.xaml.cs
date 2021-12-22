using Aurio.FFT;
using Aurio.Matching;
using Aurio.Matching.Wang2003;
using Aurio.Project;
using Aurio.Resampler;
using Aurio.Streams;
using Aurio.TaskMonitor;
using Aurio.WaveControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aurio.Test.FingerprintingWang2003
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Use PFFFT as FFT implementation
            FFTFactory.Factory = new Aurio.PFFFT.FFTFactory();
            // Use Soxr as resampler implementation
            ResamplerFactory.Factory = new Aurio.Soxr.ResamplerFactory();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressMonitor.GlobalInstance.ProcessingProgressChanged += Instance_ProcessingProgressChanged;
            ProgressMonitor.GlobalInstance.ProcessingFinished += GlobalInstance_ProcessingFinished;
        }



        private void Instance_ProcessingProgressChanged(object sender, ValueEventArgs<float> e)
        {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate
            {
                progressBar1.Value = e.Value;
            });
        }

        private void GlobalInstance_ProcessingFinished(object sender, EventArgs e)
        {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate
            {
                progressBar1.Value = 0;
            });
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Multiselect = true;
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true)
            {
                var profile = FingerprintGenerator.GetProfiles()[0];

                spectrogram1.SpectrogramSize = profile.WindowSize / 2;
                spectrogram2.SpectrogramSize = profile.WindowSize / 2;

                ColorGradient gradient = new ColorGradient(0, 1);
                gradient.AddStop(Colors.Black, 0);
                gradient.AddStop(Colors.White, 1);
                var palette = gradient.GetGradientArgbArray(1024);

                spectrogram1.ColorPalette = palette;
                spectrogram2.ColorPalette = palette;

                var store = new FingerprintStore(profile);

                Task.Factory.StartNew(() =>
                {
                    foreach (string file in dlg.FileNames)
                    {
                        AudioTrack audioTrack = new AudioTrack(new FileInfo(file));
                        IAudioStream audioStream = audioTrack.CreateAudioStream();
                        IProgressReporter progressReporter = ProgressMonitor.GlobalInstance.BeginTask("Generating fingerprints for " + audioTrack.FileInfo.Name, true);
                        int hashCount = 0;

                        FingerprintGenerator fpg = new FingerprintGenerator(profile);
                        fpg.FrameProcessed += delegate (object sender2, FrameProcessedEventArgs e2)
                        {
                            var spectrum = (float[])e2.Spectrum.Clone();
                            var spectrumResidual = (float[])e2.SpectrumResidual.Clone();
                            var peaks = new List<Aurio.Matching.Wang2003.Peak>(e2.Peaks);
                            Dispatcher.BeginInvoke((Action)delegate
                            {
                                spectrogram1.AddSpectrogramColumn(spectrum);
                                spectrogram2.AddSpectrogramColumn(spectrumResidual);
                                peaks.ForEach(peak =>
                                {
                                    spectrogram1.AddPointMarker(e2.Index, peak.Index, Colors.Red);
                                    spectrogram2.AddPointMarker(e2.Index, peak.Index, Colors.Red);
                                });
                                progressReporter.ReportProgress((double)e2.Index / e2.Indices * 100);
                            });
                        };
                        fpg.SubFingerprintsGenerated += delegate (object sender2, SubFingerprintsGeneratedEventArgs e2)
                        {
                            hashCount += e2.SubFingerprints.Count;
                            store.Add(e2);
                        };

                        fpg.Generate(audioTrack);
                        Debug.WriteLine("{0} hashes (mem {1:0.00} mb)", hashCount, (hashCount * Marshal.SizeOf(typeof(SubFingerprintHash))) / 1024f / 1024f);

                        progressReporter.Finish();
                    }
                    store.FindAllMatches();
                });

            }
        }
    }
}
