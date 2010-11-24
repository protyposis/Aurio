using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    class MultiTrackListBox : ListBox {
        
        public static readonly DependencyProperty VirtualViewportWidthProperty;

        static MultiTrackListBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiTrackListBox), new FrameworkPropertyMetadata(typeof(MultiTrackListBox)));

            VirtualViewportWidthProperty = VirtualViewBase.VirtualViewportWidthProperty.AddOwner(typeof(MultiTrackListBox), new FrameworkPropertyMetadata() { Inherits = true });
        }

        public long VirtualViewportWidth {
            get { return (long)GetValue(VirtualViewportWidthProperty); }
            set { SetValue(VirtualViewportWidthProperty, value); }
        }
    }
}
