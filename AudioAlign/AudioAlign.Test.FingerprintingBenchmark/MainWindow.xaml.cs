using AudioAlign.Audio.Matching;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.TaskMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

namespace AudioAlign.Test.FingerprintingBenchmark {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private ObservableCollection<BenchmarkEntry> benchmarkResults;

        public MainWindow() {
            InitializeComponent();
            benchmarkResults = new ObservableCollection<BenchmarkEntry>();
            dataGrid1.ItemsSource = benchmarkResults;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            ProgressMonitor.GlobalInstance.ProcessingProgressChanged += Instance_ProcessingProgressChanged;
            ProgressMonitor.GlobalInstance.ProcessingFinished += GlobalInstance_ProcessingFinished;
        }

        private void Instance_ProcessingProgressChanged(object sender, Audio.ValueEventArgs<float> e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.Value = e.Value;
            });
        }

        private void GlobalInstance_ProcessingFinished(object sender, EventArgs e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.Value = 0;
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true) {
                Benchmark(new AudioTrack(new System.IO.FileInfo(dlg.FileName)));
            }
        }

        private void ReportBenchmarkResult(BenchmarkEntry e) {
            dataGrid1.Dispatcher.BeginInvoke((Action)delegate{
                benchmarkResults.Add(e);
            });
        }

        private void Benchmark(AudioTrack track) {
            Task.Factory.StartNew(() => {
                BenchmarkHaitsmaKalker(track);
            });
        }

        private void BenchmarkHaitsmaKalker(AudioTrack track) {
            var profile = AudioAlign.Audio.Matching.HaitsmaKalker2002.FingerprintGenerator.GetProfiles()[0];
            var store = new AudioAlign.Audio.Matching.HaitsmaKalker2002.FingerprintStore(profile);
            var gen = new AudioAlign.Audio.Matching.HaitsmaKalker2002.FingerprintGenerator(profile, track, 3);

            var reporter = ProgressMonitor.GlobalInstance.BeginTask("HK-FP", true);
            int hashCount = 0;

            gen.SubFingerprintCalculated += delegate(object sender, Audio.Matching.HaitsmaKalker2002.SubFingerprintEventArgs e) {
                store.Add(e.AudioTrack, e.SubFingerprint, e.Index, e.IsVariation);
                hashCount++;
                reporter.ReportProgress((double)e.Index / e.Indices * 100);
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();

            gen.Generate();

            sw.Stop();
            ReportBenchmarkResult(new BenchmarkEntry { Track = track, Type = "HK02", HashCount = hashCount, Duration = sw.Elapsed });
        }
    }
}
