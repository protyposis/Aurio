using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AudioAlign.Audio {
    public struct PointPair {

        private Point p1, p2;

        public PointPair(double x1, double y1, double x2, double y2) {
            p1 = new Point(x1, y1);
            p2 = new Point(x2, y2);
        }

        public PointPair(Point p1, Point p2) {
            this.p1 = p1;
            this.p2 = p2;
        }

        public Point Point1 {
            get { return p1; }
            set { p1 = value; }
        }

        public Point Point2 {
            get { return p2; }
            set { p2 = value; }
        }
    }
}
