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
using System.Windows.Interop;
using System.Windows.Controls.Primitives;

namespace AudioAlign.Test.HugeControlRendering
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

        private void softwareRender_Checked(object sender, RoutedEventArgs e) {
            RenderOptions.ProcessRenderMode = (bool)((CheckBox)e.Source).IsChecked ? RenderMode.SoftwareOnly : RenderMode.Default;
            InvalidateVisual();
        }
    }
}
