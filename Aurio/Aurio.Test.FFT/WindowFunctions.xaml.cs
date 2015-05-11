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
using System.Windows.Shapes;
using Aurio.Audio;
using System.Diagnostics;

namespace Aurio.Test.FFT {
    /// <summary>
    /// Interaction logic for WindowFunctions.xaml
    /// </summary>
    public partial class WindowFunctions : Window {
        public WindowFunctions() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            sampleCountSlider.Value = 256;
        }

        private void PrintArrayToDebugOutput(float[] array) {
            string s = "";
            for (int x = 0; x < array.Length; x++) {
                s += array[x] + " ";
            }
            Debug.WriteLine(s);
        }

        private void Refresh(int sampleCount) {
            float[] samples = new float[sampleCount];
            samples = samples.Select(sample => 1f).ToArray();

            graphInput.Values = samples;

            float[] samplesRectangle = (float[])samples.Clone();
            WindowUtil.Rectangle(samplesRectangle, 0, samples.Length);
            graphRectangle.Values = samplesRectangle;

            float[] samplesTriangle = (float[])samples.Clone();
            WindowUtil.Triangle(samplesTriangle, 0, samples.Length);
            graphTriangle.Values = samplesTriangle;

            float[] samplesHamming = (float[])samples.Clone();
            WindowUtil.Hamming(samplesHamming, 0, samples.Length);
            graphHamming.Values = samplesHamming;

            float[] samplesHann = (float[])samples.Clone();
            WindowUtil.Hann(samplesHann, 0, samples.Length);
            graphHann.Values = samplesHann;

            float[] samplesBlackman = (float[])samples.Clone();
            WindowUtil.Blackman(samplesBlackman, 0, samples.Length);
            graphBlackman.Values = samplesBlackman;

            float[] samplesBlackmanHarris = (float[])samples.Clone();
            WindowUtil.BlackmanHarris(samplesBlackmanHarris, 0, samples.Length);
            graphBlackmanHarris.Values = samplesBlackmanHarris;

            float[] samplesBlackmanNuttall = (float[])samples.Clone();
            WindowUtil.BlackmanNuttall(samplesBlackmanNuttall, 0, samples.Length);
            graphBlackmanNuttall.Values = samplesBlackmanNuttall;

            float[] samplesNuttall = (float[])samples.Clone();
            WindowUtil.Nuttall(samplesNuttall, 0, samples.Length);
            graphNuttall.Values = samplesNuttall;

            //PrintArrayToDebugOutput(samples);
            //PrintArrayToDebugOutput(samplesRectangle);
            //PrintArrayToDebugOutput(samplesTriangular);
            //PrintArrayToDebugOutput(samplesHamming);
            //PrintArrayToDebugOutput(samplesHanning);
            //PrintArrayToDebugOutput(samplesBlackman);
            //PrintArrayToDebugOutput(samplesBlackmanHarris);
            //PrintArrayToDebugOutput(samplesBlackmanNuttall);
        }

        private void sampleCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!IsLoaded)
                return;

            Refresh((int)sampleCountSlider.Value);
        }
    }
}
