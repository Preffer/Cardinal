﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        public static readonly Algebra.Matrix Hermite = new Algebra.Matrix(new double[4, 4] {
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

                activePointIndex = InputLine.Points
                    .Take(InputLine.Points.Count - 1)
                    .Select((point, index) => new KeyValuePair<Point, int>(point, index))
                    .Zip<KeyValuePair<Point, int>, Point, KeyValuePair<double, int>>(
                        InputLine.Points.Skip(1),
                        (one, two) => new KeyValuePair<double, int>(
                            (Math.Abs(Vector.AngleBetween(clicked - one.Key, two - one.Key)) < 90 && Math.Abs(Vector.AngleBetween(clicked - two, one.Key - two)) < 90)
                            ? Math.Abs(Vector.CrossProduct(clicked - one.Key, clicked - two) / 2 / (one.Key - two).Length)
                            : Math.Min((clicked - one.Key).Length, (clicked - two).Length),
                            one.Value
                        )
                    ).OrderBy(pair => pair.Key).First().Value + 1;

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

        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".png";
            dialog.Filter = "PNG Image (*.png)|*.png|JPG Image (*.jpg)|*.jpg|TIF Image (*.tif)|*.tif|BMP Image (*.bmp)|*.bmp";

            if (dialog.ShowDialog() == true) {
                RenderTargetBitmap bitmap = new RenderTargetBitmap((int)Scene.ActualWidth, (int)Scene.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(Scene);
                BitmapEncoder encoder = null;
                switch (Path.GetExtension(dialog.FileName)) {
                    case ".png": {
                        encoder = new PngBitmapEncoder();
                        break;
                    }
                    case ".jpg": {
                        encoder = new JpegBitmapEncoder();
                        break;
                    }
                    case "*.tif": {
                        encoder = new TiffBitmapEncoder();
                        break;
                    }
                    case "*.bmp": {
                        encoder = new BmpBitmapEncoder();
                        break;
                    }
                }

                if (encoder != null) {
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (FileStream file = File.Create(dialog.FileName)) {
                        encoder.Save(file);
                    }
                    MessageBox.Show(FindResource("SaveBitmapSuccessText") as string + dialog.FileName, FindResource("SaveBitmapSuccessTitle") as string, MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show(FindResource("SaveBitmapFailText") as string + dialog.FileName, FindResource("SaveBitmapFailTtile") as string, MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) {
            InputLine.Points.Clear();
            SmoothLine.Points.Clear();
            activePointIndex = -1;
            emode = EditMode.None;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e) {
            string helpText = "Click left button to append point\n"
                            + "Click middle button to move the nearest point\n"
                            + "Click right button to insert point\n"
                            + "Press Escape to save pending modification\n"
                            + "Press Delete to delete the pending point";

            MessageBox.Show(helpText, "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            Matrix scaleMatrix = new Matrix();
            scaleMatrix.Scale(e.NewSize.Width / e.PreviousSize.Width, e.NewSize.Height / e.PreviousSize.Height);

            InputLine.Points = new PointCollection(InputLine.Points.Select(p => p * scaleMatrix));
            SmoothLine.Points = new PointCollection(SmoothLine.Points.Select(p => p * scaleMatrix));
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
                        for (double u = 0; u < 1.0; u += step) {
                            smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], u, tension));
                        }
                        smoothPoint.Add(Interpolation(controlPoint[i], controlPoint[i + 1], controlPoint[i + 2], controlPoint[i + 3], 1.0, tension));
                    }

                    SmoothLine.Points = smoothPoint;
                }
            }
        }

        private Point Interpolation(Point p0, Point p1, Point p2, Point p3, double u, double t) {
            Algebra.Vector uVector = new Algebra.Vector(new double[4] { Math.Pow(u, 3), Math.Pow(u, 2), u, 1 });
            Algebra.Vector uhVector = uVector * Hermite;
            Algebra.Vector pxVector = new Algebra.Vector(new double[4] { p1.X, p2.X, t * (p2.X - p0.X), t * (p3.X - p1.X) });
            Algebra.Vector pyVector = new Algebra.Vector(new double[4] { p1.Y, p2.Y, t * (p2.Y - p0.Y), t * (p3.Y - p1.Y) });

            return new Point(uhVector * pxVector, uhVector * pyVector);
        }
    }
}
