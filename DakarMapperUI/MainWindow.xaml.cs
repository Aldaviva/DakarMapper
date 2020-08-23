using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using DakarMapper;

namespace DakarMapperUI {

    public partial class MainWindow: Window {

        private readonly PositionTracker positionTracker = new PositionTracker();

        private Point minCoordinates, maxCoordinates;
        private bool hasCoordinates;

        public MainWindow() {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);

            /**/
            positionTracker.positionChanged += onPositionChanged;
            positionTracker.start();
            /*/
            // var random = new Random();
            double distance = 1;
            new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000), IsEnabled = true }.Tick += (sender, args) => {
                // onPositionChanged(this, new PointDouble(random.NextDouble() * 200 - 100, random.NextDouble() * 200 - 100));
                onPositionChanged(this, new PointDouble(distance, distance));
                distance++;
            };
            /**/
        }

        private void onPositionChanged(object sender, PointDouble position) => Dispatcher.Invoke(() => {
            Point newPosition = position.toWpfPoint();
            currentPositionDot.Center = newPosition;

            if (!points.Points.Any()) {
                pathFigure.StartPoint = newPosition;
            }

            points.Points.Add(newPosition);

            if (hasCoordinates) {
                minCoordinates.X = Math.Min(minCoordinates.X, position.x);
                minCoordinates.Y = Math.Min(minCoordinates.Y, position.y);
                maxCoordinates.X = Math.Max(maxCoordinates.X, position.x);
                maxCoordinates.Y = Math.Max(maxCoordinates.Y, position.y);
            } else {
                minCoordinates = newPosition;
                maxCoordinates = newPosition;
                hasCoordinates = true;
            }

            double thickness = Math.Max(0.1, Math.Max(maxCoordinates.X - minCoordinates.X, maxCoordinates.Y-minCoordinates.Y)) / Math.Max(Width, Height) * 6;
            Trace.WriteLine("thickness = " + thickness);
            pen.Thickness = thickness;
            currentPositionDot.RadiusX = thickness * 1.5;
            currentPositionDot.RadiusY = thickness * 1.5;
        });

    }

}