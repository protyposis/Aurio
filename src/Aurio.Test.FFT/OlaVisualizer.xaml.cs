using System.Windows;
using Aurio.FFT;

namespace Aurio.Test.FFT
{
    /// <summary>
    /// Interaction logic for OLAAnalysis.xaml
    /// </summary>
    public partial class OlaVisualizer : Window
    {
        public OlaVisualizer()
        {
            FFTFactory.Factory = new PFFFT.FFTFactory();
            InitializeComponent();
        }
    }
}
