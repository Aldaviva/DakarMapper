using System;

namespace DakarMapper.Data {

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