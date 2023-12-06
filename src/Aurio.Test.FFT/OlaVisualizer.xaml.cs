using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
