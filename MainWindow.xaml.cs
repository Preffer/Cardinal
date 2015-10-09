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

namespace Cardinal {
    public partial class MainWindow : Window {
        public static readonly Matrix Hermite = new Matrix(new double[4, 4] {
            {2, -2, 1, 1},
            {-3, 3, -2, -1},
            {0, 0, 1, 0},
            {1, 0, 0, 0}
        });

        private List<Point> inputPoint;
        private List<Point> smoothPoint;

        public MainWindow() {
            InitializeComponent();
            inputPoint = new List<Point>();
            smoothPoint = new List<Point>();
        }

        private void Scene_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ButtonState == MouseButtonState.Pressed) {
                InputLine.Points.Add(e.GetPosition(this));
                inputPoint.Add(e.GetPosition(this));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            for (double i = 0; i <= 1; i += 0.01) {
                smoothPoint.Add(Interpolation(inputPoint[0], inputPoint[1], inputPoint[2], inputPoint[3], i, 1));
            }
            InputLine.Points = new PointCollection(smoothPoint);
        }

        private Point Interpolation(Point p1, Point p2, Point p3, Point p4, double u, double t) {
            Vector uVector = new Vector(new double[4] {Math.Pow(u, 3), Math.Pow(u, 2), u, 1});
            Vector uhVector = uVector * Hermite;

            Vector pxVector = new Vector(new double[4] {p2.X, p3.X, t * (p3.X - p1.X), t * (p4.X - p2.X)});
            Vector pyVector = new Vector(new double[4] { p2.Y, p3.Y, t * (p3.Y - p1.Y), t * (p4.Y - p2.Y)});

            return new Point(uhVector * pxVector, uhVector * pyVector);
        }

    }
}
