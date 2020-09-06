using System;

namespace DakarMapper.Data {

    public readonly struct DistanceAndHeading {

        public readonly double distance;
        public readonly int    heading;

        public DistanceAndHeading(double distance, int heading) {
            this.distance = distance;
            this.heading = heading;
        }

        public bool Equals(DistanceAndHeading other) {
            return distance.Equals(other.distance) && heading.Equals(other.heading);
        }

        public static bool operator ==(DistanceAndHeading left, DistanceAndHeading right) {
            return left.Equals(right);
        }

        public static bool operator !=(DistanceAndHeading left, DistanceAndHeading right) {
            return !left.Equals(right);
        }

        public override bool Equals(object? obj) {
            return obj is DistanceAndHeading other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(distance, heading);
        }

    }

}