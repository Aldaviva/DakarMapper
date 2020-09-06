using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DakarMapper;
using DakarMapper.Data;
using Microsoft.Win32;
using ThrottleDebounce;

namespace DakarMapperUI {

    public partial class MainWindow: IDisposable {

        private const string WINDOW_POSITION_REGISTRY_NAME = "Window Position";

        private readonly PositionTracker positionTracker = new PositionTracker();
        private readonly RegistryKey     registryKey;

        private Point  minCoordinates, maxCoordinates;
        private bool   hasCoordinates;
        private double thickness = 1;

        public MainWindow() {
            InitializeComponent();

            registryKey = Registry.CurrentUser.CreateSubKey(@"Software\DakarMapper", true);
            string? previousWindowPosition = (string?) registryKey.GetValue(WINDOW_POSITION_REGISTRY_NAME);
            if (previousWindowPosition != null) {
                string[] coordinates = previousWindowPosition.Split(',');
                Left = Convert.ToDouble(coordinates[0]);
                Top = Convert.ToDouble(coordinates[1]);
                Width = Convert.ToDouble(coordinates[2]);
                Height = Convert.ToDouble(coordinates[3]);
            }

            DebouncedAction onWindowMoved = Debouncer.Debounce(() => Dispatcher.Invoke(() => registryKey.SetValue(WINDOW_POSITION_REGISTRY_NAME, string.Join(",", new[] { Left, Top, Width, Height }))),
                TimeSpan.FromMilliseconds(500));
            LocationChanged += delegate { onWindowMoved.Run(); };
            SizeChanged += delegate { onWindowMoved.Run(); };
        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);

            positionTracker.onPositionChanged += (sender,   position) => Dispatcher.Invoke(() => onPositionChanged(position));
            positionTracker.onWaypointConfirmed += (sender, position) => Dispatcher.Invoke(() => onWaypointConfirmed(position));
            positionTracker.start();
        }

        private void onPositionChanged(PointDouble position) {
            Point newPosition = position.toWpfPoint();
            newPosition.Y *= -1; //map coordinates use up is positive Y, but WPF uses down is positive Y, so flip the sign when converting
            currentPositionDot.Center = newPosition;

            if (!points.Points.Any()) {
                pathFigure.StartPoint = newPosition;
            }

            points.Points.Add(newPosition);

            if (hasCoordinates) {
                minCoordinates.X = Math.Min(minCoordinates.X, newPosition.X);
                minCoordinates.Y = Math.Min(minCoordinates.Y, newPosition.Y);
                maxCoordinates.X = Math.Max(maxCoordinates.X, newPosition.X);
                maxCoordinates.Y = Math.Max(maxCoordinates.Y, newPosition.Y);
            } else {
                minCoordinates = newPosition;
                maxCoordinates = newPosition;
                hasCoordinates = true;
            }

            thickness = Math.Max(0.1, Math.Max(maxCoordinates.X - minCoordinates.X, maxCoordinates.Y - minCoordinates.Y)) / Math.Max(Width, Height) * 6;
            Trace.WriteLine("thickness = " + thickness);
            pen.Thickness = thickness;

            foreach (Geometry geometry in dots.Children) {
                if (geometry == currentPositionDot) {
                    currentPositionDot.RadiusX = thickness * 3;
                    currentPositionDot.RadiusY = thickness * 3;
                } else if (geometry is EllipseGeometry dot) {
                    dot.RadiusX = thickness * 2.4;
                    dot.RadiusY = thickness * 2.4;
                }
            }
        }

        private void onWaypointConfirmed(PointDouble position) {
            Point wpfPoint = position.toWpfPoint();
            wpfPoint.Y *= -1;
            dots.Children.Add(new EllipseGeometry(wpfPoint, thickness, thickness));
        }

        private void clear(object sender, RoutedEventArgs e) {
            positionTracker.stop();
            hasCoordinates = false;
            thickness = 1;
            minCoordinates = default;
            maxCoordinates = default;

            points.Points.Clear();
            dots.Children.Clear();
            dots.Children.Add(currentPositionDot);
            currentPositionDot.RadiusX = 3;
            currentPositionDot.RadiusY = 3;
            positionTracker.start();
        }

        public void Dispose() {
            positionTracker.stop();
            registryKey.Dispose();
        }

    }

}