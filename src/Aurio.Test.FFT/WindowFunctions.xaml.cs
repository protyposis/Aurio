using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics;
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
using Aurio;

namespace Aurio.Test.FFT
{
    /// <summary>
    /// Interaction logic for WindowFunctions.xaml
    /// </summary>
    public partial class WindowFunctions : Window
    {
        public ObservableCollection<WindowFunctionViewModel> WindowFunctionModels { get; }

        public WindowFunctions()
        {
            WindowFunctionModels = new ObservableCollection<WindowFunctionViewModel>();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sampleCountSlider.Value = 256;
        }

        private void PrintArrayToDebugOutput(float[] array)
        {
            string s = "";
            for (int x = 0; x < array.Length; x++)
            {
                s += array[x] + " ";
            }
            Debug.WriteLine(s);
        }

        private void Refresh(int sampleCount)
        {
            WindowFunctionModels.Clear();

            Enum.GetValues(typeof(WindowType))
                .Cast<WindowType>()
                .Select(
                    windowType =>
                        new WindowFunctionViewModel(WindowUtil.GetFunction(windowType, sampleCount))
                )
                .ToList()
                .ForEach(vm => WindowFunctionModels.Add(vm));

            //PrintArrayToDebugOutput(samples);
            //PrintArrayToDebugOutput(samplesRectangle);
            //PrintArrayToDebugOutput(samplesTriangular);
            //PrintArrayToDebugOutput(samplesHamming);
            //PrintArrayToDebugOutput(samplesHanning);
            //PrintArrayToDebugOutput(samplesBlackman);
            //PrintArrayToDebugOutput(samplesBlackmanHarris);
            //PrintArrayToDebugOutput(samplesBlackmanNuttall);
        }

        private void sampleCountSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (!IsLoaded)
                return;

            Refresh((int)sampleCountSlider.Value);
        }
    }
}
