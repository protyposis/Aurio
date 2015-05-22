// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
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
