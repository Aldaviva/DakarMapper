using System;
using DakarMapper;
using DakarMapper.Data;
using FakeItEasy;
using Xunit;

namespace Tests {

    public class PositionTrackerTests {

        private readonly HeadUpDisplayScraper headUpDisplayScraper = A.Fake<HeadUpDisplayScraper>();

        private PointDouble mostRecentPosition;

        public PositionTrackerTests() {
            var positionTracker = new PositionTracker(headUpDisplayScraper);
            positionTracker.onPositionChanged += (sender, position) => mostRecentPosition = position;
            positionTracker.start();
        }

        private void onDistanceOrHeadingChanged(double distance, int heading) {
            headUpDisplayScraper.onDistanceOrHeadingChanged += Raise.FreeForm.With(null, new DistanceAndHeading(distance, heading));
        }

        [Fact]
        public void trackPosition() {
            onDistanceOrHeadingChanged(1, 0);
            Assert.Equal(0, mostRecentPosition.x, 2);
            Assert.Equal(1, mostRecentPosition.y, 2);

            onDistanceOrHeadingChanged(1 + Math.Sqrt(2), 45);
            Assert.Equal(1, mostRecentPosition.x, 2);
            Assert.Equal(2, mostRecentPosition.y, 2);
        }

        [Fact]
        public void slop() {
            onDistanceOrHeadingChanged(5, 90);
            Assert.Equal(5, mostRecentPosition.x, 2);
            Assert.Equal(0, mostRecentPosition.y, 2);

            onDistanceOrHeadingChanged(10, 90);
            Assert.Equal(10, mostRecentPosition.x, 2);
            Assert.Equal(0, mostRecentPosition.y, 2);

            onDistanceOrHeadingChanged(8, 90);
            Assert.Equal(10, mostRecentPosition.x, 2);
            Assert.Equal(0, mostRecentPosition.y, 2);

            onDistanceOrHeadingChanged(9, 90);
            Assert.Equal(11, mostRecentPosition.x, 2);
            Assert.Equal(0, mostRecentPosition.y, 2);

            onDistanceOrHeadingChanged(10, 90);
            Assert.Equal(12, mostRecentPosition.x, 2);
            Assert.Equal(0, mostRecentPosition.y, 2);

        }

    }

}