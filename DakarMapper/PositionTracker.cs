using System;

namespace DakarMapper {

    public class PositionTracker {

        public delegate void PositionChangedEvent(object sender, PointDouble position);

        public event PositionChangedEvent? positionChanged;

        private readonly DistanceAndHeadingTracker distanceAndHeadingTracker = new DistanceAndHeadingTracker();

        private PointDouble mostRecentPosition = PointDouble.EMPTY;
        private double mostRecentDistance = 0;

        public void start() {
            distanceAndHeadingTracker.onDistanceOrHeadingChanged += onDistanceOrHeadingChanged;
            distanceAndHeadingTracker.start();
        }

        private void onDistanceOrHeadingChanged(object sender, DistanceAndHeadingTracker.DistanceAndHeading args) {
            double headingRadians = args.heading * Math.PI / 180;
            double distanceTraveledSinceLastChange = args.distance - mostRecentDistance;
            var distanceTraveled = new PointDouble(distanceTraveledSinceLastChange * Math.Sin(headingRadians), distanceTraveledSinceLastChange * -Math.Cos(headingRadians));

            PointDouble oldPosition = mostRecentPosition;
            mostRecentPosition = new PointDouble(mostRecentPosition.x + distanceTraveled.x, mostRecentPosition.y + distanceTraveled.y);
            mostRecentDistance = args.distance;

            if (oldPosition != mostRecentPosition) {
                positionChanged?.Invoke(this, mostRecentPosition);
            }
        }

    }

    public readonly struct PointDouble {

        public static readonly PointDouble EMPTY = new PointDouble(0, 0);

        public double x { get; }
        public double y { get; }

        public PointDouble(double x, double y) {
            this.x = x;
            this.y = y;
        }

        public bool Equals(PointDouble other) {
            return x.Equals(other.x) && y.Equals(other.y);
        }

        public override bool Equals(object? obj) {
            return obj is PointDouble other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(x, y);
        }

        public static bool operator ==(PointDouble left, PointDouble right) {
            return left.Equals(right);
        }

        public static bool operator !=(PointDouble left, PointDouble right) {
            return !left.Equals(right);
        }

    }

}