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
using AudioAlign.Audio;
using AudioAlign.Audio.Streams;
using AudioAlign.Audio.Matching;

namespace AudioAlign.Test.Fingerprinting {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private FingerprintStore store;
        private Profile profile;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            ProgressMonitor.GlobalInstance.ProcessingProgressChanged += GlobalInstance_ProcessingProgressChanged;
            ProgressMonitor.GlobalInstance.ProcessingFinished += GlobalInstance_ProcessingFinished;

            trackListBox.SelectionChanged += new SelectionChangedEventHandler(trackListBox_SelectionChanged);
            trackFingerprintListBox.SelectionChanged += new SelectionChangedEventHandler(trackFingerprintListBox_SelectionChanged);
        }

        void GlobalInstance_ProcessingProgressChanged(object sender, Audio.ValueEventArgs<float> e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.Value = e.Value;
            });
        }

        void GlobalInstance_ProcessingFinished(object sender, EventArgs e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.Value = 0;
            });
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Multiselect = true;
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true) {
                trackListBox.Items.Clear();

                profile = FingerprintGenerator.GetProfiles()[0];
                store = new FingerprintStore(profile);
                store.Threshold = 0.45f;

                Task.Factory.StartNew(() => Parallel.ForEach<string>(dlg.FileNames,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                fileName => {
                    AudioTrack audioTrack = new AudioTrack(new FileInfo(fileName));
                    IProgressReporter progressReporter = ProgressMonitor.GlobalInstance.BeginTask("Generating sub-fingerprints for " + audioTrack.FileInfo.Name, true);

                    FingerprintGenerator fpg = new FingerprintGenerator(profile, audioTrack);
                    int subFingerprintsCalculated = 0;
                    fpg.SubFingerprintsGenerated += new EventHandler<SubFingerprintsGeneratedEventArgs>(delegate(object s2, SubFingerprintsGeneratedEventArgs e2) {
                        subFingerprintsCalculated++;
                        progressReporter.ReportProgress((double)e2.Index / e2.Indices * 100);
                        store.Add(e2);
                    });

                    fpg.Generate();
                    //store.Analyze();
                    progressReporter.Finish();
                }))
                .ContinueWith(task => {
                    foreach (AudioTrack audioTrack in store.AudioTracks.Keys) {
                        trackListBox.Items.Add(audioTrack);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void trackListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            trackFingerprintListBox.Items.Clear();
            if (trackListBox.SelectedItems.Count > 0) {
                AudioTrack audioTrack = (AudioTrack)trackListBox.SelectedItem;
                Dictionary<SubFingerprintHash, object> hashFilter = new Dictionary<SubFingerprintHash, object>(); // helper structure to filter out duplicate hashes
                foreach (SubFingerprintHash hash in store.AudioTracks[audioTrack]) {
                    if (store.CollisionMap.GetValues(hash).Count > 1 && !hashFilter.ContainsKey(hash)) {
                        // only add hash to the list if it points to at least two different audio tracks
                        List<SubFingerprintLookupEntry> entries = store.CollisionMap.GetValues(hash);
                        SubFingerprintLookupEntry firstEntry = entries[0];
                        for (int x = 1; x < entries.Count; x++) {
                            if (entries[x].AudioTrack != firstEntry.AudioTrack) {
                                trackFingerprintListBox.Items.Add(hash);
                                hashFilter.Add(hash, null);
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
                SubFingerprintHash hash = (SubFingerprintHash)trackFingerprintListBox.SelectedItem;
                foreach (SubFingerprintLookupEntry lookupEntry in store.CollisionMap.GetValues(hash)) {
                    fingerprintMatchListBox.Items.Add(lookupEntry);
                }
            }
        }

        private void btnFindMatches_Click(object sender, RoutedEventArgs e) {
            if (trackFingerprintListBox.SelectedItems.Count > 0) {
                SubFingerprintHash hash = (SubFingerprintHash)trackFingerprintListBox.SelectedItem;
                var matches = store.FindMatches(hash);
                ListMatches(matches);
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
                ShowFingerprints((SubFingerprintLookupEntry)fingerprintMatchListBox.SelectedItems[0],
                    (SubFingerprintLookupEntry)fingerprintMatchListBox.SelectedItems[1]);
            }
        }

        private void btnFindAllMatches_Click(object sender, RoutedEventArgs e) {
            List<Match> matches = store.FindAllMatches();
            ListMatches(matches);
        }

        private void ListMatches(List<Match> matches) {
            //PrintMatchResult(matches);
            matchGrid.ItemsSource = matches;
        }

        private void matchGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Match match = matchGrid.SelectedItem as Match;
            if (match == null) {
                return;
            }
            int index1 = (int)Math.Round((double)match.Track1Time.Ticks / TimeUtil.SECS_TO_TICKS * profile.HashTimeScale);
            int index2 = (int)Math.Round((double)match.Track2Time.Ticks / TimeUtil.SECS_TO_TICKS * profile.HashTimeScale);
            ShowFingerprints(new SubFingerprintLookupEntry(match.Track1, index1),
                new SubFingerprintLookupEntry(match.Track2, index2));
        }

        private void ShowFingerprints(SubFingerprintLookupEntry sfp1, SubFingerprintLookupEntry sfp2) {
            Fingerprint fp1 = store.GetFingerprint(sfp1);
            Fingerprint fp2 = store.GetFingerprint(sfp2);
            Fingerprint fpDifference = fp1.Difference(fp2);
            fingerprintView1.Fingerprint = fp1;
            fingerprintView2.Fingerprint = fp2;
            fingerprintView3.Fingerprint = fpDifference;
            berLabel.Content = Fingerprint.CalculateBER(fp1, fp2);
        }
    }
}
