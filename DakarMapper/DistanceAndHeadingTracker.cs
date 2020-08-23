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
    public class DistanceAndHeadingTracker {

        private static readonly int[] DISTANCE_OFFSETS = { 0x4194890, 0x1B0, 0xA0, 0x20, 0x20, 0x5A0, 0x28, 0x68, 0x410 };
        private static readonly int[] HEADING_OFFSETS  = { 0x4194890, 0x1B0, 0xA0, 0x20, 0x20, 0x398, 0x28, 0x68, 0x410 };

        public event DistanceOrHeadingChangedEvent? onDistanceOrHeadingChanged;

        public delegate void DistanceOrHeadingChangedEvent(object sender, DistanceAndHeading args);

        private DistanceAndHeading mostRecentDistanceAndHeading = new DistanceAndHeading(0, 0);

        private bool started = false;

        public void start() => Task.Run(() => {
            if (started) {
                return;
            } else {
                started = true;
            }

            var distanceStringBuffer = new byte["9999.99 KM".Length * 2];
            var headingStringBuffer = new byte["C. 360º".Length * 2];

            while (started) {
                using Process dakarProcess = Process.GetProcessesByName("Dakar18Game-Win64-Shipping").FirstOrDefault();
                if (dakarProcess != null) {
                    IntPtr dakarProcessHandle = OpenProcess(ProcessAccessFlags.VIRTUAL_MEMORY_READ, false, dakarProcess.Id);
                    while (started) {
                        try {
                            while (started) {
                                IntPtr distanceAddress = getMemoryAddressByOffsetChain(dakarProcessHandle, dakarProcess.MainModule, DISTANCE_OFFSETS);
                                IntPtr headingAddress = getMemoryAddressByOffsetChain(dakarProcessHandle, dakarProcess.MainModule, HEADING_OFFSETS);

                                Array.Clear(distanceStringBuffer, 0, distanceStringBuffer.Length);
                                Array.Clear(headingStringBuffer, 0, headingStringBuffer.Length);

                                ReadProcessMemory(dakarProcessHandle, distanceAddress, distanceStringBuffer, distanceStringBuffer.Length, out IntPtr distanceBytesRead);
                                ReadProcessMemory(dakarProcessHandle, headingAddress, headingStringBuffer, headingStringBuffer.Length, out IntPtr headingBytesRead);

                                if (distanceBytesRead.ToInt64() == 0 || distanceBytesRead.ToInt64() > distanceStringBuffer.Length
                                 || headingBytesRead.ToInt64() == 0 || headingBytesRead.ToInt64() > headingStringBuffer.Length) {
                                    Console.WriteLine("read wrong number of bytes for string");
                                    break;
                                }

                                string distanceString = Encoding.Unicode.GetString(distanceStringBuffer, 0, distanceBytesRead.ToInt32()).Split((char) 0, 2)[0];
                                string headingString = Encoding.Unicode.GetString(headingStringBuffer, 0, headingBytesRead.ToInt32()).Split((char) 0, 2)[0];

                                double distance = double.Parse(distanceString.Split(' ', 2)[0]);
                                int heading = int.Parse(headingString.Split(' ', 2)[1].TrimEnd('º'));

                                var newDistanceAndHeading = new DistanceAndHeading(distance, heading);

                                if (newDistanceAndHeading != mostRecentDistanceAndHeading) {
                                    mostRecentDistanceAndHeading = newDistanceAndHeading;
                                    onDistanceOrHeadingChanged?.Invoke(this, mostRecentDistanceAndHeading);
                                }

                                //Console.WriteLine("{0}\t{1}", distanceString, headingString);
                                Thread.Sleep(500);
                            }

                            Console.WriteLine("memory addresses are wrong, recalculating...");
                        } catch (ApplicationException) {
                            Console.WriteLine("program is running, but could not find memory addresses");
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

        /// <summary>Read the values of a chain of pointers from a process's memory, adding </summary>
        /// <param name="processHandle">the output from calling <c>OpenProcess()</c></param>
        /// <param name="offsets">enumerable of byte offsets to add to pointer values which are read from the process's memory. should not end in 0x0. should start with the offset from the module's baseaddress.</param>
        /// <param name="module">typically <c>Process.MainModule</c></param>
        /// <exception cref="ApplicationException">if one of the memory addresses could not be read</exception>
        private static IntPtr getMemoryAddressByOffsetChain(IntPtr processHandle, ProcessModule module, IEnumerable<int> offsets) {
            IntPtr memoryAddress = module.BaseAddress;
            var memoryValue = new byte[IntPtr.Size];
            foreach (int offset in offsets) {
                memoryAddress = IntPtr.Add(memoryAddress, offset);
                bool success = ReadProcessMemory(processHandle, memoryAddress, memoryValue, IntPtr.Size, out IntPtr bytesRead);
                if (!success) {
                    throw new ApplicationException($"Could not read memory address 0x{memoryAddress.ToInt64():X}: {Marshal.GetLastWin32Error()}");
                }

                long pointerValue = BitConverter.ToInt64(memoryValue, 0);
                memoryAddress = new IntPtr(pointerValue);
            }

            return memoryAddress;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [Flags]
        private enum ProcessAccessFlags: uint {

            ALL = 0x001F0FFF,
            TERMINATE = 0x00000001,
            CREATE_THREAD = 0x00000002,
            VIRTUAL_MEMORY_OPERATION = 0x00000008,
            VIRTUAL_MEMORY_READ = 0x00000010,
            VIRTUAL_MEMORY_WRITE = 0x00000020,
            DUPLICATE_HANDLE = 0x00000040,
            CREATE_PROCESS = 0x000000080,
            SET_QUOTA = 0x00000100,
            SET_INFORMATION = 0x00000200,
            QUERY_INFORMATION = 0x00000400,
            QUERY_LIMITED_INFORMATION = 0x00001000,
            SYNCHRONIZE = 0x00100000

        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hProcess);

        public readonly struct DistanceAndHeading {

            public readonly double distance;
            public readonly int heading;

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