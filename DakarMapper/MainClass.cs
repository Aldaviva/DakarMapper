using System;
using System.Threading;

namespace DakarMapper {

    public class MainClass {

        private static readonly ManualResetEvent READY_TO_EXIT = new ManualResetEvent(false);

        public static void Main() {
            var distanceAndHeadingTracker = new DistanceAndHeadingTracker();
            distanceAndHeadingTracker.onDistanceOrHeadingChanged += (sender, args) => Console.WriteLine($"{args.distance:N2} km, {args.heading:N0}°");
            distanceAndHeadingTracker.start();

            READY_TO_EXIT.WaitOne();
        }

    }

}