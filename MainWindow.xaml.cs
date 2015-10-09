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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace Cardinal {
    public partial class MainWindow : Window, INotifyPropertyChanged {
        public static readonly Matrix Hermite = new Matrix(new double[4, 4] {
            {2, -2, 1, 1},
            {-3, 3, -2, -1},
            {0, 0, 1, 0},
            {1, 0, 0, 0}
        });

        private List<Point> inputPoint;

        public MainWindow() {
            InitializeComponent();
            inputPoint = new List<Point>();
        }

        private void Scene_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ButtonState == MouseButtonState.Pressed) {
                InputLine.Points.Add(e.GetPosition(this));
                inputPoint.Add(e.GetPosition(this));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            List<Point> controlPoint = new List<Point>();
            List<Point> smoothPoint= new List<Point>();

            controlPoint.Add(inputPoint.First());
            controlPoint.AddRange(inputPoint);
            controlPoint.Add(inputPoint.Last());
            
            int count = controlPoint.Count() - 3;

            for (int i = 0; i < count; i++) {
                for (double u = 0; u <= 1; u += 0.01) {
                    smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], u, 1));
                }
            }

            SmoothLine.Points = new PointCollection(smoothPoint);
        }

        private Point Interpolation(Point p1, Point p2, Point p3, Point p4, double u, double t) {
            Vector uVector = new Vector(new double[4] {Math.Pow(u, 3), Math.Pow(u, 2), u, 1});
            Vector uhVector = uVector * Hermite;
            Vector pxVector = new Vector(new double[4] {p2.X, p3.X, t * (p3.X - p1.X), t * (p4.X - p2.X)});
            Vector pyVector = new Vector(new double[4] {p2.Y, p3.Y, t * (p3.Y - p1.Y), t * (p4.Y - p2.Y)});
            
            return new Point(uhVector * pxVector, uhVector * pyVector);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
