using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Aurio.Test.HugeControlRendering
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

        private void softwareRender_Checked(object sender, RoutedEventArgs e)
        {
            RenderOptions.ProcessRenderMode = (bool)((CheckBox)e.Source).IsChecked
                ? RenderMode.SoftwareOnly
                : RenderMode.Default;
            InvalidateVisual();
        }
    }
}
