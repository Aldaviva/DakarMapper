using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DakarMapper {

    /// <summary>
    /// CheatEngine pointer scan for UTF-16 string with max 3 different offsets per node (default) and max pointer level 9 (2 more than default 7)
    /// </summary>
    public class HeadUpDisplayScraper {

        private static readonly int[] DISTANCE_OFFSETS  = { 0x4194890, 0x1B0, 0xA0, 0x20, 0x20, 0x5A0, 0x28, 0x68, 0x410 };
        private static readonly int[] HEADING_OFFSETS   = { 0x4194890, 0x1B0, 0xA0, 0x20, 0x20, 0x398, 0x28, 0x68, 0x410 };
        private static readonly int[] WAYPOINTS_OFFSETS = { 0x41930A0, 0x1A0, 0xF8, 0x6C8, 0x78, 0x210, 0x28, 0x68, 0x210 };

        public delegate void DistanceOrHeadingChangedEvent(object sender, DistanceAndHeading args);

        public event DistanceOrHeadingChangedEvent? onDistanceOrHeadingChanged;

        public delegate void WaypointsChangedEvent(object sender, int waypointsOk);

        public event WaypointsChangedEvent? onWaypointsChanged;

        private DistanceAndHeading mostRecentDistanceAndHeading = new DistanceAndHeading(0, 0);
        private int                mostRecentWaypoints          = 0;

        private bool started = false;

        public void start() => Task.Run(() => {
            if (started) {
                return;
            } else {
                started = true;
            }

            while (started) {
                using Process dakarProcess = Process.GetProcessesByName("Dakar18Game-Win64-Shipping").FirstOrDefault();
                if (dakarProcess != null) {
                    IntPtr dakarProcessHandle = OpenProcess(ProcessAccessFlags.VIRTUAL_MEMORY_READ, false, dakarProcess.Id);
                    while (started) {
                        try {
                            while (started) {
                                double distance = double.Parse(readStringFromProcessMemory(dakarProcessHandle, dakarProcess.MainModule, DISTANCE_OFFSETS, "9999.99 KM".Length)
                                    .Split(' ', 2)[0]);
                                int heading = int.Parse(readStringFromProcessMemory(dakarProcessHandle, dakarProcess.MainModule, HEADING_OFFSETS, "C. 360º".Length)
                                    .Split(' ', 2)[1].TrimEnd('º'));
                                int waypoints = int.Parse(readStringFromProcessMemory(dakarProcessHandle, dakarProcess.MainModule, WAYPOINTS_OFFSETS, "999/999".Length)
                                    .Split('/', 2)[0]);

                                var newDistanceAndHeading = new DistanceAndHeading(distance, heading);

                                if (newDistanceAndHeading != mostRecentDistanceAndHeading) {
                                    mostRecentDistanceAndHeading = newDistanceAndHeading;
                                    onDistanceOrHeadingChanged?.Invoke(this, mostRecentDistanceAndHeading);
                                }

                                if (mostRecentWaypoints != waypoints) {
                                    mostRecentWaypoints = waypoints;
                                    onWaypointsChanged?.Invoke(this, waypoints);
                                }

                                //Console.WriteLine("{0}\t{1}", distanceString, headingString);
                                Thread.Sleep(500);
                            }

                            Console.WriteLine("memory addresses are wrong, recalculating...");
                        } catch (ApplicationException e) {
                            Console.WriteLine("program is running, but could not find memory addresses: " + e.Message);
                        } catch (Win32Exception e) {
                            if (e.NativeErrorCode == 299) {
                                break;
                            } else {
                                Console.WriteLine(e);
                            }
                        }

                        Thread.Sleep(2000);
                    }

                    CloseHandle(dakarProcessHandle);
                }

                Console.WriteLine("process is not running");
                Thread.Sleep(2000);
            }
        });

        public void stop() {
            started = false;
        }

        private string readStringFromProcessMemory(IntPtr processHandle, ProcessModule module, IEnumerable<int> offsets, int maxCharacters) {
            byte[] stringBuffer = new byte[maxCharacters * 2];

            IntPtr stringAddress = getMemoryAddressByOffsetChain(processHandle, module, offsets);

            bool success = ReadProcessMemory(processHandle, stringAddress, stringBuffer, stringBuffer.Length, out long bytesRead);
            if (!success || bytesRead == 0 || bytesRead > stringBuffer.Length) {
                throw new ApplicationException("read wrong number of bytes for string");
            }

            return Encoding.Unicode.GetString(stringBuffer, 0, stringBuffer.Length).Split((char) 0, 2)[0];
        }

        /// <summary>Read the values of a chain of pointers from a process's memory, adding the next offset to the pointer value</summary>
        /// <param name="processHandle">the output from calling <c>OpenProcess()</c></param>
        /// <param name="offsets">enumerable of byte offsets to add to pointer values which are read from the process's memory. should not end in 0x0. should start with the offset from the module's baseaddress.</param>
        /// <param name="module">typically <c>Process.MainModule</c></param>
        /// <exception cref="ApplicationException">if one of the memory addresses could not be read</exception>
        private static IntPtr getMemoryAddressByOffsetChain(IntPtr processHandle, ProcessModule module, IEnumerable<int> offsets) {
            IntPtr memoryAddress = module.BaseAddress;
            foreach (int offset in offsets) {
                memoryAddress = IntPtr.Add(memoryAddress, offset);
                bool success = ReadProcessMemory(processHandle, memoryAddress, out IntPtr memoryValue, Marshal.SizeOf<long>(), out long bytesRead);
                if (!success) {
                    throw new ApplicationException($"Could not read memory address 0x{memoryAddress.ToInt64():X}: {Marshal.GetLastWin32Error()}");
                }

                memoryAddress = memoryValue;
            }

            return memoryAddress;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [Flags]
        private enum ProcessAccessFlags: uint {

            ALL                       = 0x001F0FFF,
            TERMINATE                 = 0x00000001,
            CREATE_THREAD             = 0x00000002,
            VIRTUAL_MEMORY_OPERATION  = 0x00000008,
            VIRTUAL_MEMORY_READ       = 0x00000010,
            VIRTUAL_MEMORY_WRITE      = 0x00000020,
            DUPLICATE_HANDLE          = 0x00000040,
            CREATE_PROCESS            = 0x00000080,
            SET_QUOTA                 = 0x00000100,
            SET_INFORMATION           = 0x00000200,
            QUERY_INFORMATION         = 0x00000400,
            QUERY_LIMITED_INFORMATION = 0x00001000,
            SYNCHRONIZE               = 0x00100000

        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out long lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, out IntPtr lpBuffer, int dwSize, out long lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hProcess);

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

}