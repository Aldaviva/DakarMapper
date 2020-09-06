using System;
using DakarMapper.Data;

namespace DakarMapper {

    public class PositionTracker {

        public event PositionChangedEvent? onPositionChanged;
        public delegate void PositionChangedEvent(object sender, PointDouble position);

        public event WaypointConfirmedEvent? onWaypointConfirmed;
        public delegate void WaypointConfirmedEvent(object sender, PointDouble position);

        private readonly HeadUpDisplayScraper headUpDisplayScraper;

        private PointDouble mostRecentPosition = PointDouble.EMPTY;
        private double      mostRecentDistance = 0;

        public PositionTracker(HeadUpDisplayScraper headUpDisplayScraper) {
            this.headUpDisplayScraper = headUpDisplayScraper;
        }

        public PositionTracker(): this(new HeadUpDisplayScraperImpl()) { }

        public void start() {
            headUpDisplayScraper.onDistanceOrHeadingChanged += onDistanceOrHeadingChanged;
            headUpDisplayScraper.onWaypointsChanged += onWaypointsChanged;
            headUpDisplayScraper.start();
        }

        public void stop() {
            headUpDisplayScraper.stop();
            mostRecentPosition = PointDouble.EMPTY;
            mostRecentDistance = 0;
        }

        private void onDistanceOrHeadingChanged(object? sender, DistanceAndHeading args) {
            double headingRadians = args.heading * Math.PI / 180;
            double distanceTraveledSinceLastChange = args.distance - mostRecentDistance;
            Console.WriteLine($"Position: traveled {distanceTraveledSinceLastChange:N2} km since last update");
            if (distanceTraveledSinceLastChange > 0) {
                var distanceTraveled = new PointDouble(distanceTraveledSinceLastChange * Math.Sin(headingRadians), distanceTraveledSinceLastChange * Math.Cos(headingRadians));

                PointDouble oldPosition = mostRecentPosition;
                mostRecentPosition = new PointDouble(mostRecentPosition.x + distanceTraveled.x, mostRecentPosition.y + distanceTraveled.y);
                Console.WriteLine($"Position: moved to ({mostRecentPosition.x:N2}, {mostRecentPosition.y:N2})");

                if (oldPosition != mostRecentPosition) {
                    onPositionChanged?.Invoke(this, mostRecentPosition);
                }
            }

            mostRecentDistance = args.distance;
        }

        private void onWaypointsChanged(object sender, int waypointsOk) {
            onWaypointConfirmed?.Invoke(this, mostRecentPosition);
        }

    }

}