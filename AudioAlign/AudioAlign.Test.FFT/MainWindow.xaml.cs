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
using AudioAlign.Audio;
using AudioAlign.Audio.Streams;
using Exocortex.DSP;
using System.Diagnostics;

namespace AudioAlign.Test.FFT {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e) {
            WindowFunctions wf = new WindowFunctions();
            wf.Show();
        }

        private void button2_Click(object sender, RoutedEventArgs e) {
            // FFT resources:
            // http://www.mathworks.de/support/tech-notes/1700/1702.html
            // http://stackoverflow.com/questions/6627288/audio-spectrum-analysis-using-fft-algorithm-in-java
            // http://stackoverflow.com/questions/1270018/explain-the-fft-to-me
            // http://stackoverflow.com/questions/6666807/how-to-scale-fft-output-of-wave-file

            float frequency = float.Parse(frequencyTextBox.Text);
            int ws = (int)windowSize.Value;
            float frequencyFactor = ws / frequency;

            SineGeneratorStream sine = new SineGeneratorStream(44100, frequency, new TimeSpan(0, 0, 1));

            float[] input = new float[ws];
            sine.Read(input, 0, ws);
            
            //// add second frequency
            //SineGeneratorStream sine2 = new SineGeneratorStream(44100, 700, new TimeSpan(0, 0, 1));
            //float[] input2 = new float[ws];
            //sine2.Read(input2, 0, ws);
            //for (int x = 0; x < input.Length; x++) {
            //    input[x] += input2[x];
            //    input[x] /= 2;
            //}

            //// add dc offset
            //for (int x = 0; x < input.Length; x++) {
            //    input[x] += 0.5f;
            //}

            inputGraph.Values = input;

            WindowFunction wf = WindowUtil.GetFunction((WindowType)windowTypes.SelectedValue, ws);
            float[] window = (float[])input.Clone();
            wf.Apply(window);

            input2Graph.Values = window;

            float[] fftIO = (float[])window.Clone();
            FFTUtil.FFT(fftIO);

            //// convert real input to complex input with im part set to zero
            //float[] fftIO = new float[ws * 2];
            //int i = 0;
            //for (int j = 0; j < window.Length; j++) {
            //    fftIO[i] = window[j];
            //    i += 2;
            //}
            //Fourier.FFT(fftIO, fftIO.Length / 2, FourierDirection.Forward);

            fftOutputGraph.Values = fftIO;


            float[] fftResult = new float[ws / 2];
            //FFTUtil.Results(fftIO, fftResult);

            // transform complex output to magnitudes
            int y = 0;
            float sum = 0;
            for (int x = 0; x < fftIO.Length; x += 2) { //  / 2
                fftResult[y] = FFTUtil.CalculateMagnitude(fftIO[x], fftIO[x + 1]) / ws * 2;
                sum += fftResult[y++];
            }

            //// adjust values for the sum to become 1
            //float sum2 = 0;
            //for (int x = 0; x < fftResult.Length; x++) {
            //    fftResult[x] /= sum;
            //    sum2 += fftResult[x];
            //}
            //Debug.WriteLine("sum / sum2: {0} / {1}", sum, sum2);

            fftNormalizedOutputGraph.Values = fftResult;

            // convert magnitudes to decibel
            float[] fftResultdB = new float[fftResult.Length];
            for (int x = 0; x < fftResult.Length; x++) {
                fftResultdB[x] = (float)VolumeUtil.LinearToDecibel(fftResult[x]);
            }
            fftdBOutputGraph.Values = fftResultdB;

            summary.Text = String.Format(
                "input min/max: {0}/{1} -> window min/max: {2}/{3} -> fft min/max: {4}/{5} -> result min/max/bins/sum: {6}/{7}/{8}/{9} -> dB min/max: {10:0.000000}/{11:0.000000}",
                input.Min(), input.Max(),
                window.Min(), window.Max(),
                fftIO.Min(), fftIO.Max(),
                fftResult.Min(), fftResult.Max(), fftResult.Length, sum,
                fftResultdB.Min(), fftResultdB.Max());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
        }
    }
}
