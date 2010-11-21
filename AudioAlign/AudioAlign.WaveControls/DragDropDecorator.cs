using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    /// <summary>
    /// CURRENTLY NOT WORKING FOR LISTBOX!!!!!!!!!!!!
    /// </summary>
    public class DragDropDecorator : ContentControl {

        private static readonly DependencyPropertyKey IsDragOverPropertyKey;
        public static readonly DependencyProperty IsDragOverProperty;
        
        static DragDropDecorator() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DragDropDecorator), new FrameworkPropertyMetadata(typeof(DragDropDecorator)));

            IsDragOverPropertyKey = DependencyProperty.RegisterReadOnly("IsDragOver", typeof(bool), typeof(DragDropDecorator),
                new FrameworkPropertyMetadata());
            IsDragOverProperty = IsDragOverPropertyKey.DependencyProperty;
        }

        private bool draggingArmed;
        private bool draggingActive;

        public DragDropDecorator() {
            this.AllowDrop = true;
        }

        public bool IsDragOver {
            get { return (bool)GetValue(IsDragOverProperty); }
            private set { SetValue(IsDragOverPropertyKey, value); }
        }

        //protected override void OnPreviewMouseDown(System.Windows.Input.MouseButtonEventArgs e) {
        //    base.OnPreviewMouseDown(e);
        //    Debug.WriteLine("DragDropDecorator OnPreviewMouseDown");
        //    draggingArmed = true;
        //}

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e) {
            base.OnMouseDown(e);
            //Debug.WriteLine("DragDropDecorator OnMouseDown");
            draggingArmed = true;
        }

        //protected override void OnPreviewMouseUp(System.Windows.Input.MouseButtonEventArgs e) {
        //    base.OnPreviewMouseUp(e);
        //    Debug.WriteLine("DragDropDecorator OnPreviewMouseUp");
        //    draggingArmed = false;
        //}

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e) {
            base.OnMouseUp(e);
            //Debug.WriteLine("DragDropDecorator OnMouseUp");
            draggingArmed = false;
        }

        protected override void OnPreviewMouseMove(System.Windows.Input.MouseEventArgs e) {
            base.OnPreviewMouseMove(e);
            //Debug.WriteLine("DragDropDecorator OnPreviewMouseMove");

            //if (draggingArmed && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && !draggingActive) {
            //    draggingArmed = false;
            //    draggingActive = true;
            //    DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
            //    draggingActive = false;
            //    e.Handled = true;
            //}
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) {
            base.OnMouseMove(e);
            //Debug.WriteLine("DragDropDecorator OnMouseMove");

            if (draggingArmed && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && !draggingActive) {
                draggingArmed = false;
                draggingActive = true;
                DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
                draggingActive = false;
                e.Handled = true;
            }
        }

        protected override void OnDragEnter(DragEventArgs e) {
            base.OnDragEnter(e);

            DragDropDecorator source = GetDropSource(e);
            if(source != null && source != this) {
                IsDragOver = true;
            }
        }

        protected override void OnDragLeave(DragEventArgs e) {
            base.OnDragLeave(e);
            IsDragOver = false;
        }

        protected override void OnDrop(DragEventArgs e) {
            base.OnDrop(e);

            DragDropDecorator source = GetDropSource(e);
            DragDropDecorator target = this;
            SwapContent(source, target);

            IsDragOver = false;
        }

        private DragDropDecorator GetDropSource(DragEventArgs e) {
            return e.Data.GetData(typeof(DragDropDecorator)) as DragDropDecorator;
        }

        private void SwapContent(DragDropDecorator d1, DragDropDecorator d2) {
            object c1 = d1.Content;
            d1.RemoveLogicalChild(c1);

            object c2 = d2.Content;
            d2.RemoveLogicalChild(c2);

            d1.Content = c2;
            d2.Content = c1;
        }
    }
}
