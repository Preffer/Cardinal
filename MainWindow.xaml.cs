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
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Cardinal {
    public enum DisplayMode {
        Both,
        Input,
        Smooth
    }

    public partial class MainWindow : Window, INotifyPropertyChanged {
        public static readonly Matrix Hermite = new Matrix(new double[4, 4] {
            {2, -2, 1, 1},
            {-3, 3, -2, -1},
            {0, 0, 1, 0},
            {1, 0, 0, 0}
        });

        private Brush inputLineColor = Brushes.Cyan;
        private Brush smoothLineColor = Brushes.Crimson;
        private DisplayMode mode = DisplayMode.Both;
        private double tension = 1;
        private int grain = 20;

        public MainWindow() {
            InitializeComponent();

            Display.ItemsSource = Enum.GetValues(typeof(DisplayMode));
            DataContext = this;
            PropertyChanged += new PropertyChangedEventHandler(SceneChanged);
        }

        public Brush InputLineColor {
            get {
                return inputLineColor;
            }
            set {
                inputLineColor = value;
                NotifyPropertyChanged("InputLineColor");
            }
        }

        public Brush SmoothLineColor {
            get {
                return smoothLineColor;
            }
            set {
                smoothLineColor = value;
                NotifyPropertyChanged("SmoothLineColor");
            }
        }

        public DisplayMode Mode {
            get {
                return mode;
            }
            set {
                mode = value;
                NotifyPropertyChanged("Mode");
                NotifyPropertyChanged("ShowInputLine");
                NotifyPropertyChanged("ShowSmoothLine");
            }
        }

        public double Tension {
            get {
                return tension;
            }
            set {
                tension = value;
                NotifyPropertyChanged("Tension");
                NotifyPropertyChanged("Scene");
            }
        }

        public int Grain {
            get {
                return grain;
            }
            set {
                grain = value;
                NotifyPropertyChanged("Grain");
                NotifyPropertyChanged("Scene");
            }
        }

        public Visibility ShowInputLine {
            get {
                return (mode == DisplayMode.Input || mode == DisplayMode.Both) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public Visibility ShowSmoothLine {
            get {
                return (mode == DisplayMode.Smooth || mode == DisplayMode.Both) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Scene_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ButtonState == MouseButtonState.Pressed) {
                InputLine.Points.Add(e.GetPosition(Scene));
                NotifyPropertyChanged("Scene");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) {
            InputLine.Points.Clear();
            SmoothLine.Points.Clear();
        }

        private void SceneChanged(object sender, PropertyChangedEventArgs e) {
            Console.WriteLine(e.PropertyName);

            if (e.PropertyName == "Scene") {
                List<Point> controlPoint = new List<Point>();
                List<Point> smoothPoint = new List<Point>();

                controlPoint.Add(InputLine.Points.First());
                controlPoint.AddRange(InputLine.Points);
                controlPoint.Add(InputLine.Points.Last());

                int count = controlPoint.Count() - 3;
                double step = 1.0 / grain;
                for (int i = 0; i < count; i++) {
                    for (double u = 0; u <= 1; u += step) {
                        smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], u, tension));
                    }
                }

                SmoothLine.Points = new PointCollection(smoothPoint);
            }
        }

        private Point Interpolation(Point p1, Point p2, Point p3, Point p4, double u, double t) {
            Vector uVector = new Vector(new double[4] { Math.Pow(u, 3), Math.Pow(u, 2), u, 1 });
            Vector uhVector = uVector * Hermite;
            Vector pxVector = new Vector(new double[4] { p2.X, p3.X, t * (p3.X - p1.X), t * (p4.X - p2.X) });
            Vector pyVector = new Vector(new double[4] { p2.Y, p3.Y, t * (p3.Y - p1.Y), t * (p4.Y - p2.Y) });

            return new Point(uhVector * pxVector, uhVector * pyVector);
        }
    }
}
