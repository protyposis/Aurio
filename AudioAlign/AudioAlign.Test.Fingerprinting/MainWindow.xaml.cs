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
using AudioAlign.Audio.Streams;
using AudioAlign.Audio.Matching;

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
            ProgressMonitor.GlobalInstance.ProcessingProgressChanged += new EventHandler<Audio.ValueEventArgs<float>>(Instance_ProcessingProgressChanged);
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
            dlg.Multiselect = true;
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true) {
                foreach (string file in dlg.FileNames) {
                    AudioTrack audioTrack = new AudioTrack(new FileInfo(file));
                    IAudioStream audioStream = audioTrack.CreateAudioStream();

                    Task.Factory.StartNew(() => {
                        ProgressReporter progressReporter = ProgressMonitor.GlobalInstance.BeginTask("Generating sub-fingerprints for " + audioTrack.FileInfo.Name, true);

                        FingerprintGenerator fpg = new FingerprintGenerator(audioTrack, 3, true);
                        int subFingerprintsCalculated = 0;
                        fpg.SubFingerprintCalculated += new EventHandler<SubFingerprintEventArgs>(delegate(object s2, SubFingerprintEventArgs e2) {
                            subFingerprintsCalculated++;
                            progressReporter.ReportProgress((double)e2.Timestamp.Ticks / audioTrack.Length.Ticks * 100);
                            store.Add(e2.AudioTrack, e2.SubFingerprint, e2.Timestamp, e2.IsVariation);
                        });

                        fpg.Generate();
                        //store.Analyze();
                        ProgressMonitor.GlobalInstance.EndTask(progressReporter);
                    });
                }
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
                Dictionary<SubFingerprint, object> hashFilter = new Dictionary<SubFingerprint, object>(); // helper structure to filter out duplicate subfingerprints
                foreach (SubFingerprint sfp in store.AudioTracks[audioTrack]) {
                    if (store.LookupTable[sfp].Count > 1 && !hashFilter.ContainsKey(sfp)) {
                        // only add subfingerprints to the list if it points to at least two different audio tracks
                        SubFingerprintLookupEntry firstEntry = store.LookupTable[sfp][0];
                        for (int x = 1; x < store.LookupTable[sfp].Count; x++) {
                            if (store.LookupTable[sfp][x].AudioTrack != firstEntry.AudioTrack) {
                                trackFingerprintListBox.Items.Add(sfp);
                                hashFilter.Add(sfp, null);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void trackFingerprintListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            fingerprintMatchListBox.Items.Clear();
            if (trackFingerprintListBox.SelectedItems.Count > 0) {
                SubFingerprint subFingerprint = (SubFingerprint)trackFingerprintListBox.SelectedItem;
                foreach (SubFingerprintLookupEntry lookupEntry in store.LookupTable[subFingerprint]) {
                    fingerprintMatchListBox.Items.Add(lookupEntry);
                }
            }
        }

        private void btnFindMatches_Click(object sender, RoutedEventArgs e) {
            if (trackFingerprintListBox.SelectedItems.Count > 0) {
                SubFingerprint subFingerprint = (SubFingerprint)trackFingerprintListBox.SelectedItem;
                PrintMatchResult(store.FindMatches(subFingerprint));
            }
        }

        private void PrintMatchResult(List<Match> matches) {
            Debug.WriteLine("MATCHES:");
            foreach (Match match in matches) {
                Debug.WriteLine(match.Track1.Name + "@" + match.Track1Time + " <=> " +
                    match.Track2.Name + "@" + match.Track2Time + ": " + match.Similarity);
            }
            Debug.WriteLine(matches.Count + " matches total");
        }

        private void fingerprintMatchListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (fingerprintMatchListBox.SelectedItems.Count == 2) {
                ShowFingerprints(fingerprintMatchListBox.SelectedItems[0] as SubFingerprintLookupEntry,
                    fingerprintMatchListBox.SelectedItems[1] as SubFingerprintLookupEntry);
            }
        }

        private void btnStats_Click(object sender, RoutedEventArgs e) {
            store.PrintStats();
        }

        private void btnFindAllMatches_Click(object sender, RoutedEventArgs e) {
            List<Match> matches = store.FindAllMatches();
            PrintMatchResult(matches);
            matchGrid.ItemsSource = matches;
        }

        private void btnFindAllMatchingMatches_Click(object sender, RoutedEventArgs e) {
            List<Match> matches = store.FindAllMatchingMatches();
            PrintMatchResult(matches);
            matchGrid.ItemsSource = matches;
        }

        private void matchGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Match match = matchGrid.SelectedItem as Match;
            if (match == null) {
                return;
            }
            int index1 = FingerprintGenerator.TimeStampToSubFingerprintIndex(match.Track1Time);
            int index2 = FingerprintGenerator.TimeStampToSubFingerprintIndex(match.Track2Time);
            ShowFingerprints(new SubFingerprintLookupEntry(match.Track1, index1),
                new SubFingerprintLookupEntry(match.Track2, index2));
        }

        private void ShowFingerprints(SubFingerprintLookupEntry sfp1, SubFingerprintLookupEntry sfp2) {
            Fingerprint fp1 = store.GetFingerprint(sfp1);
            Fingerprint fp2 = store.GetFingerprint(sfp2);
            Fingerprint fpDifference = fp1.Difference(fp2);
            fingerprintView1.SubFingerprints = fp1;
            fingerprintView2.SubFingerprints = fp2;
            fingerprintView3.SubFingerprints = fpDifference;
            berLabel.Content = store.CalculateBER(fp1, fp2);
        }
    }
}
