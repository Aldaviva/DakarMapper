using System.Threading;

namespace DakarMapper {

    public static class MainClass {

        private static readonly ManualResetEvent READY_TO_EXIT = new ManualResetEvent(false);

        public static void Main() {
            var positionTracker = new PositionTracker();
            positionTracker.start();

            READY_TO_EXIT.WaitOne();
        }

    }

}