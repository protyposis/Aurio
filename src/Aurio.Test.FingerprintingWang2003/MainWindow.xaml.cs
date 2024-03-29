﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Aurio.FFT;
using Aurio.Matching;
using Aurio.Matching.Wang2003;
using Aurio.Project;
using Aurio.Resampler;
using Aurio.Streams;
using Aurio.TaskMonitor;
using Aurio.WaveControls;

namespace Aurio.Test.FingerprintingWang2003
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Profile profile;
        private FingerprintStore store;

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
            ProgressMonitor.GlobalInstance.ProcessingProgressChanged +=
                Instance_ProcessingProgressChanged;
            ProgressMonitor.GlobalInstance.ProcessingFinished += GlobalInstance_ProcessingFinished;

            profile = FingerprintGenerator.GetProfiles()[0];
            store = new FingerprintStore(profile);
        }

        private void Instance_ProcessingProgressChanged(object sender, ValueEventArgs<float> e)
        {
            progressBar1
                .Dispatcher
                .BeginInvoke(
                    (Action)
                        delegate
                        {
                            progressBar1.Value = e.Value;
                        }
                );
        }

        private void GlobalInstance_ProcessingFinished(object sender, EventArgs e)
        {
            progressBar1
                .Dispatcher
                .BeginInvoke(
                    (Action)
                        delegate
                        {
                            progressBar1.Value = 0;
                        }
                );
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Multiselect = true;
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true)
            {
                spectrogram1.SpectrogramSize = profile.WindowSize / 2;
                spectrogram2.SpectrogramSize = profile.WindowSize / 2;

                ColorGradient gradient = new ColorGradient(0, 1);
                gradient.AddStop(Colors.Black, 0);
                gradient.AddStop(Colors.White, 1);
                var palette = gradient.GetGradientArgbArray(1024);

                spectrogram1.ColorPalette = palette;
                spectrogram2.ColorPalette = palette;

                Task.Factory.StartNew(() =>
                {
                    foreach (string file in dlg.FileNames)
                    {
                        AudioTrack audioTrack = new AudioTrack(new FileInfo(file));
                        IAudioStream audioStream = audioTrack.CreateAudioStream();
                        IProgressReporter progressReporter = ProgressMonitor
                            .GlobalInstance
                            .BeginTask(
                                "Generating fingerprints for " + audioTrack.FileInfo.Name,
                                true
                            );
                        int hashCount = 0;
                        long columnOffset = spectrogram1.ColumnCount;
                        int lastFrameIndex = 0;
                        float[] silentSpectrum = null;

                        FingerprintGenerator fpg = new FingerprintGenerator(profile);
                        fpg.FrameProcessed += delegate(object sender2, FrameProcessedEventArgs e2)
                        {
                            var spectrum = (float[])e2.Spectrum.Clone();
                            var spectrumResidual = (float[])e2.SpectrumResidual.Clone();
                            var peaks = new List<Aurio.Matching.Wang2003.Peak>(e2.Peaks);
                            var skippedFrames = e2.Index - lastFrameIndex - 1;
                            lastFrameIndex = e2.Index;
                            silentSpectrum ??= Enumerable
                                .Repeat(float.MinValue, e2.Spectrum.Length)
                                .ToArray();

                            Dispatcher.BeginInvoke(
                                (Action)
                                    delegate
                                    {
                                        if (skippedFrames > 0)
                                        {
                                            // Draw spectrogram frames that were skipped by the fingerprint generator because
                                            // their power was below the profile's threshold.
                                            Debug.WriteLine(
                                                $"filling {skippedFrames} skipped frames"
                                            );
                                            for (var f = 1; f <= skippedFrames; f++)
                                            {
                                                spectrogram1.AddSpectrogramColumn(silentSpectrum);
                                                spectrogram2.AddSpectrogramColumn(silentSpectrum);
                                            }
                                        }

                                        spectrogram1.AddSpectrogramColumn(spectrum);
                                        spectrogram2.AddSpectrogramColumn(spectrumResidual);
                                        peaks.ForEach(peak =>
                                        {
                                            spectrogram1.AddPointMarker(
                                                columnOffset + e2.Index,
                                                peak.Index,
                                                Colors.Red
                                            );
                                            spectrogram2.AddPointMarker(
                                                columnOffset + e2.Index,
                                                peak.Index,
                                                Colors.Red
                                            );
                                        });
                                        progressReporter.ReportProgress(
                                            (double)e2.Index / e2.Indices * 100
                                        );
                                    }
                            );
                        };
                        fpg.PeakPairsGenerated += (_, e2) =>
                        {
                            var peakPairs = new List<PeakPair>(e2.PeakPairs);
                            Dispatcher.BeginInvoke(() =>
                            {
                                peakPairs.ForEach(pair =>
                                {
                                    spectrogram1.AddLineMarker(
                                        columnOffset + pair.Index,
                                        pair.Peak1.Index,
                                        columnOffset + pair.Index + pair.Distance,
                                        pair.Peak2.Index,
                                        Colors.Green
                                    );
                                    spectrogram2.AddLineMarker(
                                        columnOffset + pair.Index,
                                        pair.Peak1.Index,
                                        columnOffset + pair.Index + pair.Distance,
                                        pair.Peak2.Index,
                                        Colors.Green
                                    );
                                });
                            });
                        };
                        fpg.SubFingerprintsGenerated += delegate(
                            object sender2,
                            SubFingerprintsGeneratedEventArgs e2
                        )
                        {
                            hashCount += e2.SubFingerprints.Count;
                            store.Add(e2);
                        };

                        fpg.Generate(audioTrack);
                        Debug.WriteLine(
                            "{0} hashes (mem {1:0.00} mb)",
                            hashCount,
                            (hashCount * Marshal.SizeOf(typeof(SubFingerprintHash))) / 1024f / 1024f
                        );

                        progressReporter.Finish();
                    }
                    store.FindAllMatches();
                });
            }
        }
    }
}
