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
using AudioAlign.Audio.Matching.HaitsmaKalker2002;
using System.IO;
using AudioAlign.Audio.Project;
using System.Threading.Tasks;
using AudioAlign.Audio.TaskMonitor;
using System.Diagnostics;

namespace AudioAlign.Test.Fingerprinting {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private FingerprintStore store;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            ProgressMonitor.Instance.ProcessingProgressChanged += new EventHandler<Audio.ValueEventArgs<float>>(Instance_ProcessingProgressChanged);
            store = new FingerprintStore();

            trackListBox.SelectionChanged += new SelectionChangedEventHandler(trackListBox_SelectionChanged);
            trackFingerprintListBox.SelectionChanged += new SelectionChangedEventHandler(trackFingerprintListBox_SelectionChanged);
        }

        void Instance_ProcessingProgressChanged(object sender, Audio.ValueEventArgs<float> e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.Value = e.Value;
            });
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true) {
                AudioTrack audioTrack = new AudioTrack(new FileInfo(dlg.FileName));
                long trackSamples = audioTrack.CreateAudioStream().SampleCount / 4 / 2;

                Task.Factory.StartNew(() => {
                    ProgressReporter progressReporter = ProgressMonitor.Instance.BeginTask("Generating sub-fingerprints for " + audioTrack.FileInfo.Name, true);

                    FingerprintGenerator fpg = new FingerprintGenerator(audioTrack);
                    int subFingerprintsCalculated = 0;
                    fpg.SubFingerprintCalculated += new EventHandler<SubFingerprintEventArgs>(delegate(object s2, SubFingerprintEventArgs e2) {
                        subFingerprintsCalculated++;
                        progressReporter.ReportProgress(100 / ((double)trackSamples / (subFingerprintsCalculated * 64)));
                        store.Add(e2.AudioTrack, e2.SubFingerprint);
                    });

                    fpg.Generate();
                    store.Analyze();
                    ProgressMonitor.Instance.EndTask(progressReporter);
                });
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e) {
            trackListBox.Items.Clear();
            foreach (AudioTrack audioTrack in store.AudioTracks.Keys) {
                trackListBox.Items.Add(audioTrack);
            }
        }

        private void trackListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            trackFingerprintListBox.Items.Clear();
            if (trackListBox.SelectedItems.Count > 0) {
                AudioTrack audioTrack = (AudioTrack)trackListBox.SelectedItem;
                SubFingerprint prevSfp = new SubFingerprint(0);
                foreach (SubFingerprint sfp in store.AudioTracks[audioTrack]) {
                    if (sfp != prevSfp && store.LookupTable[sfp].Count > 1) {
                        trackFingerprintListBox.Items.Add(sfp);
                    }
                    prevSfp = sfp;
                }
            }
        }

        private void trackFingerprintListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            fingerprintMatchListBox.Items.Clear();
            if (trackFingerprintListBox.SelectedItems.Count > 0) {
                SubFingerprint subFingerprint = (SubFingerprint)trackFingerprintListBox.SelectedItem;
                foreach (FingerprintStore.SubFingerprintLookupEntry lookupEntry in store.LookupTable[subFingerprint]) {
                    fingerprintMatchListBox.Items.Add(lookupEntry);
                }
            }
        }
    }
}
