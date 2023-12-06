using System;
using System.IO;
using System.Linq;
using System.Windows;
using Aurio.Streams;
using Exocortex.DSP;

namespace Aurio.Test.FFT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            WindowFunctions wf = new WindowFunctions();
            wf.Show();
        }

        private void olaButton_Click(object sender, RoutedEventArgs e)
        {
            var ola = new OlaVisualizer();
            ola.Show();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Exception missingLibraryException = null;
            FFTLibrary fftLib = (FFTLibrary)fftLibComboBox.SelectedValue;

            try
            {
                Generate(fftLib);
            }
            catch (DllNotFoundException ex)
            {
                // Account for native DLLs
                missingLibraryException = ex;
            }
            catch (FileNotFoundException ex)
            {
                // Account for .NET assemblies
                missingLibraryException = ex;
            }
            catch (BadImageFormatException ex)
            {
                // Account for invalid files (e.g. dummy files)
                missingLibraryException = ex;
            }

            if (missingLibraryException != null)
            {
                fftOutputGraph.Reset();
                fftNormalizedOutputGraph.Reset();
                fftdBOutputGraph.Reset();
                MessageBox.Show(
                    this,
                    missingLibraryException.Message,
                    fftLib + ": Library not found!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Generate(FFTLibrary fftLib)
        {
            // FFT resources:
            // http://www.mathworks.de/support/tech-notes/1700/1702.html
            // http://stackoverflow.com/questions/6627288/audio-spectrum-analysis-using-fft-algorithm-in-java
            // http://stackoverflow.com/questions/1270018/explain-the-fft-to-me
            // http://stackoverflow.com/questions/6666807/how-to-scale-fft-output-of-wave-file

            /*
             * FFT max value test:
             * 16Hz, 1024 samplerate, 512 samples, rectangle window
             * -> input min/max: -1/1
             * -> FFT max: 256
             * -> FFT result max: ~1
             * -> FFT dB result max: ~1
             *
             * source: "Windowing Functions Improve FFT Results, Part I"
             * => kann zum Normalisieren für Fensterfunktionen verwendet werden
             */

            float frequency = float.Parse(frequencyTextBox.Text);
            int ws = (int)windowSize.Value;
            float frequencyFactor = ws / frequency;
            int sampleRate = int.Parse(sampleRateTextBox.Text);

            SineGeneratorStream sine = new SineGeneratorStream(
                sampleRate,
                frequency,
                new TimeSpan(0, 0, 1)
            );

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

            if (fftLib == FFTLibrary.ExocortexDSP)
            {
                // This function call indirection is needed to allow this method execute even when the
                // Exocortex FFT assembly is missing.
                ApplyExocortexDSP(fftIO);
            }
            else if (fftLib == FFTLibrary.FFTW)
            {
                FFTW.FFTW fftw = new FFTW.FFTW(fftIO.Length);
                fftw.Execute(fftIO);
            }
            else if (fftLib == FFTLibrary.PFFFT)
            {
                PFFFT.PFFFT pffft = new PFFFT.PFFFT(fftIO.Length, PFFFT.Transform.Real);
                pffft.Forward(fftIO, fftIO);
            }
            else if (fftLib == FFTLibrary.FftSharp)
            {
                FftSharp.FFT fft = new FftSharp.FFT(fftIO.Length);
                fft.Forward(fftIO);
            }

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
            float sum = 0;
            int y = 0;
            for (int x = 0; x < fftIO.Length; x += 2)
            { //  / 2
                fftResult[y] = FFTUtil.CalculateMagnitude(fftIO[x], fftIO[x + 1]) / ws * 2;
                sum += fftResult[y++];
            }
            //FFTUtil.Results(fftIO, fftResult);

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
            //for (int x = 0; x < fftResult.Length; x++) {
            //    fftResultdB[x] = (float)VolumeUtil.LinearToDecibel(fftResult[x]);
            //}
            FFTUtil.Results(fftIO, fftResultdB);
            fftdBOutputGraph.Values = fftResultdB;

            summary.Text = String.Format(
                "input min/max: {0}/{1} -> window min/max: {2}/{3} -> fft min/max: {4}/{5} -> result min/max/bins/sum: {6}/{7}/{8}/{9} -> dB min/max: {10:0.000000}/{11:0.000000}",
                input.Min(),
                input.Max(),
                window.Min(),
                window.Max(),
                fftIO.Min(),
                fftIO.Max(),
                fftResult.Min(),
                fftResult.Max(),
                fftResult.Length,
                sum,
                fftResultdB.Min(),
                fftResultdB.Max()
            );
        }

        private void ApplyExocortexDSP(float[] samples)
        {
            Fourier.RFFT(samples, FourierDirection.Forward);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) { }
    }
}
