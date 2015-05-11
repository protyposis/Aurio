using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Aurio.WaveControls {
    public class FormattedAutoTooltipSlider : Slider {

        private ToolTip autoToolTip;
        private String autoToolTipFormat;
        
        public FormattedAutoTooltipSlider() : base() {}

        public String AutoToolTipFormat {
            get { return autoToolTipFormat; }
            set { autoToolTipFormat = value; }
        }

        private void FormatAutoToolTipContent() {
            if (autoToolTip == null) {
                FieldInfo field = typeof(Slider).GetField("_autoToolTip", BindingFlags.NonPublic | BindingFlags.Instance);
                autoToolTip = field.GetValue(this) as ToolTip;
            }

            if (autoToolTip != null && !String.IsNullOrEmpty(autoToolTipFormat)) {
                autoToolTip.Content = String.Format(autoToolTipFormat, autoToolTip.Content);
            }
        }

        protected override void OnThumbDragStarted(System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            base.OnThumbDragStarted(e);
            FormatAutoToolTipContent();
        }

        protected override void OnThumbDragDelta(System.Windows.Controls.Primitives.DragDeltaEventArgs e) {
            base.OnThumbDragDelta(e);
            FormatAutoToolTipContent();
        }
    }
}
