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
            int ws = (int)windowSize.Value;
            float[] input = new float[ws];

            for (int x = 0; x < ws; x++) {
                input[x] = (float)Math.Sin(x);
            }

            inputGraph.Values = input;

            WindowFunction wf = WindowUtil.GetFunction((WindowType)windowTypes.SelectedValue, ws);
            float[] input2 = (float[])input.Clone();
            wf.Apply(input2);

            input2Graph.Values = input2;

            float[] fftInput = (float[])input2.Clone();
            FFTUtil.FFT(fftInput);
            float[] fftOutput = new float[ws / 2];
            FFTUtil.NormalizeResults(fftInput, fftOutput);

            outputGraph.Values = fftOutput;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
        }
    }
}
