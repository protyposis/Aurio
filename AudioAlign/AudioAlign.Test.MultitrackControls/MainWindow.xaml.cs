﻿using System;
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
using AudioAlign.Audio.Project;
using System.IO;
using AudioAlign.WaveControls;
using AudioAlign.Audio;
using System.Diagnostics;

namespace AudioAlign.Test.MultitrackControls {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void btnAddWaveform_Click(object sender, RoutedEventArgs e) {
            // http://msdn.microsoft.com/en-us/library/aa969773.aspx
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "audio"; // Default file name
            dlg.DefaultExt = ".wav"; // Default file extension
            dlg.Filter = "Wave files (.wav)|*.wav"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true) {
                // Open document
                AudioTrack audioTrack = new AudioTrack(new FileInfo(dlg.FileName));
                //WaveView waveView = new WaveView();
                //waveView.AudioStream = AudioStreamFactory.FromAudioTrackForGUI(audioTrack);
                //multiTrackPanel.Children.Add(waveView);
                multiTrackViewer1.Items.Add(AudioStreamFactory.FromAudioTrackForGUI(audioTrack));
            }
        }
    }
}