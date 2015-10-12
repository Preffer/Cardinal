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

    public enum EditMode {
        None,
        Append,
        Move,
        Insert
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
        private DisplayMode dmode = DisplayMode.Both;
        private EditMode emode = EditMode.None;
        private double tension = 1;
        private int grain = 20;
        private double thickness = 2;
        private int activePointIndex = -1;

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
            PropertyChanged += new PropertyChangedEventHandler(SceneChanged);
        }

        public DisplayMode DMode {
            get {
                return dmode;
            }
            set {
                dmode = value;
                NotifyPropertyChanged("DMode");
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
            }
        }

        public int Grain {
            get {
                return grain;
            }
            set {
                grain = value;
                NotifyPropertyChanged("Grain");
            }
        }

        public double Thickness {
            get {
                return thickness;
            }
            set {
                thickness = value;
                NotifyPropertyChanged("Thickness");
            }
        }

        public Visibility ShowInputLine {
            get {
                return (dmode == DisplayMode.Input || dmode == DisplayMode.Both) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public Visibility ShowSmoothLine {
            get {
                return (dmode == DisplayMode.Smooth || dmode == DisplayMode.Both) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Scene_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                if (InputLine.Points.Count == 0) {
                    InputLine.Points.Add(e.GetPosition(Scene));
                }
                InputLine.Points.Add(e.GetPosition(Scene));
                NotifyPropertyChanged("InputLine");

                activePointIndex = InputLine.Points.Count - 1;
                emode = EditMode.Append;
            }
            if (e.MiddleButton == MouseButtonState.Pressed && InputLine.Points.Count > 0) {
                Point clicked = e.GetPosition(Scene);
                activePointIndex = InputLine.Points.Select((point, index) => new KeyValuePair<Point, int>(point, index)).OrderBy(pair => (clicked - pair.Key).LengthSquared).First().Value;
                emode = EditMode.Move;
            }
            if (e.RightButton == MouseButtonState.Pressed && InputLine.Points.Count >= 2) {
                Point clicked = e.GetPosition(Scene);
                activePointIndex = InputLine.Points.Select((point, index) => new KeyValuePair<Point, int>(point, index)).OrderBy(pair => (clicked - pair.Key).LengthSquared).Take(2).OrderBy(pair => pair.Value).Last().Value;
                InputLine.Points.Insert(activePointIndex, clicked);
                NotifyPropertyChanged("InputLine");
                emode = EditMode.Insert;
            }
        }

        private void Scene_MouseUp(object sender, MouseButtonEventArgs e) {
            if (emode != EditMode.Append) {
                activePointIndex = -1;
                emode = EditMode.None;
            }
        }

        private void Scene_MouseMove(object sender, MouseEventArgs e) {
            if (activePointIndex != -1) {
                InputLine.Points.RemoveAt(activePointIndex);
                InputLine.Points.Insert(activePointIndex, e.GetPosition(Scene));
                NotifyPropertyChanged("InputLine");
            }
        }

        private void Scene_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape: {
                    activePointIndex = -1;
                    emode = EditMode.None;
                    break;
                }
                case Key.Delete: {
                    if (activePointIndex != -1) {
                        InputLine.Points.RemoveAt(activePointIndex);
                        NotifyPropertyChanged("InputLine");

                        activePointIndex = -1;
                        emode = EditMode.None;
                    }
                    break;
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) {
            InputLine.Points.Clear();
            SmoothLine.Points.Clear();
            activePointIndex = -1;
            emode = EditMode.None;
        }

        private void SceneChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "InputLine" || e.PropertyName == "Tension" || e.PropertyName == "Grain") {
                if (InputLine.Points.Count > 0) {
                    List<Point> controlPoint = new List<Point>();
                    PointCollection smoothPoint = new PointCollection();

                    controlPoint.Add(InputLine.Points.First());
                    controlPoint.AddRange(InputLine.Points);
                    controlPoint.Add(InputLine.Points.Last());

                    int count = controlPoint.Count - 3;
                    double step = 1.0 / grain;
                    for (int i = 0; i < count; i++) {
                        for (double u = 0; u < 1.0 ; u += step) {
                            smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], u, tension));
                        }
                        smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], 1.0, tension));
                    }

                    SmoothLine.Points = smoothPoint;
                }
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
