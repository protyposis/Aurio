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
using AudioAlign.Audio.Matching;
using System.IO;

namespace AudioAlign.Test.Fingerprinting {
    /// <summary>
    /// Interaction logic for FingerprintView.xaml
    /// </summary>
    public partial class FingerprintView : UserControl {

        private BitmapSource bitmap;

        public FingerprintView() {
            InitializeComponent();
        }

        public Fingerprint Fingerprint {
            get { return (Fingerprint)GetValue(FingerprintProperty); }
            set { SetValue(FingerprintProperty, value); }
        }

        public static readonly DependencyProperty FingerprintProperty =
            DependencyProperty.Register("Fingerprint", typeof(Fingerprint), typeof(FingerprintView), new UIPropertyMetadata(null,
                new PropertyChangedCallback(OnFingerprintChanged)));

        public static void OnFingerprintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            FingerprintView fv = (FingerprintView)d;

            int width = 32;
            int height = fv.Fingerprint.Length;
            int dpi = 96;
            byte[] pixelData = new byte[width * height];

            int index = 0;
            foreach (SubFingerprintHash hash in fv.Fingerprint) {
                for (int x = 0; x < 32; x++) {
                    pixelData[index++] = (byte)(hash[x] ? 0 : 255);
                }
            }

            fv.bitmap = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Gray8, null, pixelData, width);
            fv.bitmapDisplay.Source = fv.bitmap;
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e) {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files|*.png";
            if (bitmap != null && dlg.ShowDialog() == true) {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                String photolocation = dlg.FileName;
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var filestream = new FileStream(photolocation, FileMode.Create)) {
                    encoder.Save(filestream);
                }
            }
        }
    }
}
