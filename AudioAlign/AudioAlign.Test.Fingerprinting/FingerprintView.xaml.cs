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
using AudioAlign.Audio.Matching.HaitsmaKalker2002;
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

        public Fingerprint SubFingerprints {
            get { return (Fingerprint)GetValue(SubFingerprintsProperty); }
            set { SetValue(SubFingerprintsProperty, value); }
        }

        public static readonly DependencyProperty SubFingerprintsProperty =
            DependencyProperty.Register("SubFingerprints", typeof(Fingerprint), typeof(FingerprintView), new UIPropertyMetadata(null,
                new PropertyChangedCallback(OnSubFingerprintsChanged)));

        public static void OnSubFingerprintsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            FingerprintView fv = (FingerprintView)d;

            int width = 32;
            int height = fv.SubFingerprints.Length;
            int dpi = 96;
            byte[] pixelData = new byte[width * height];

            int index = 0;
            foreach (SubFingerprint sfp in fv.SubFingerprints) {
                for (int x = 0; x < 32; x++) {
                    pixelData[index++] = (byte)(sfp[x] ? 0 : 255);
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
