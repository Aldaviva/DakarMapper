using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DakarMapper.Data;

namespace DakarMapper {

    public interface HeadUpDisplayScraper {

        public delegate void DistanceOrHeadingChangedEvent(object sender, DistanceAndHeading args);
        event DistanceOrHeadingChangedEvent? onDistanceOrHeadingChanged;

        public delegate void WaypointsChangedEvent(object sender, int waypointsOk);
        event WaypointsChangedEvent? onWaypointsChanged;

        void start();

        void stop();

    }

    /// <summary>
    /// CheatEngine pointer scan for UTF-16 string with max 3 different offsets per node (default) and max pointer level 9 (2 more than default 7)
    /// </summary>
    public class HeadUpDisplayScraperImpl: HeadUpDisplayScraper {

        private static readonly int[] DISTANCE_OFFSETS  = { 0x4194890, 0x1B0, 0xA0, 0x20, 0x20, 0x5A0, 0x28, 0x68, 0x410 };
        private static readonly int[] HEADING_OFFSETS   = { 0x4194890, 0x1B0, 0xA0, 0x20, 0x20, 0x398, 0x28, 0x68, 0x410 };
        private static readonly int[] WAYPOINTS_OFFSETS = { 0x41930A0, 0x1A0, 0xF8, 0x6C8, 0x78, 0x210, 0x28, 0x68, 0x210 };

        public event HeadUpDisplayScraper.DistanceOrHeadingChangedEvent? onDistanceOrHeadingChanged;
        public event HeadUpDisplayScraper.WaypointsChangedEvent? onWaypointsChanged;

        private DistanceAndHeading       mostRecentDistanceAndHeading = new DistanceAndHeading(0, 0);
        private int                      mostRecentWaypoints;
        private CancellationTokenSource? cts;

        public void start() {
            if (cts == null) {
                cts = new CancellationTokenSource();
            } else {
                return;
            }

            Task.Run(async () => {
                while (!cts.IsCancellationRequested) {
                    using Process dakarProcess = Process.GetProcessesByName("Dakar18Game-Win64-Shipping").FirstOrDefault();
                    if (dakarProcess != null) {
                        IntPtr dakarProcessHandle = Win32.OpenProcess(Win32.ProcessAccessFlags.VIRTUAL_MEMORY_READ, false, dakarProcess.Id);
                        while (!cts.IsCancellationRequested) {
                            try {
                                while (!cts.IsCancellationRequested) {
                                    double distance = double.Parse(readStringFromProcessMemory(dakarProcessHandle, dakarProcess.MainModule, DISTANCE_OFFSETS, "9999.99 KM".Length)
                                        .Split(' ', 2)[0]);
                                    int heading = int.Parse(readStringFromProcessMemory(dakarProcessHandle, dakarProcess.MainModule, HEADING_OFFSETS, "C. 360º".Length)
                                        .Split(' ', 2)[1].TrimEnd('º'));
                                    int waypoints = int.Parse(readStringFromProcessMemory(dakarProcessHandle, dakarProcess.MainModule, WAYPOINTS_OFFSETS, "999/999".Length)
                                        .Split('/', 2)[0]);

                                    var newDistanceAndHeading = new DistanceAndHeading(distance, heading);

                                    if (mostRecentDistanceAndHeading.distance != newDistanceAndHeading.distance) {
                                        mostRecentDistanceAndHeading = newDistanceAndHeading;
                                        onDistanceOrHeadingChanged?.Invoke(this, newDistanceAndHeading);
                                    }

                                    Console.WriteLine($"HUD: {mostRecentDistanceAndHeading.distance:N2} km, {mostRecentDistanceAndHeading.heading:N0} °");

                                    if (mostRecentWaypoints != waypoints) {
                                        mostRecentWaypoints = waypoints;
                                        onWaypointsChanged?.Invoke(this, waypoints);
                                    }

                                    //Console.WriteLine("{0}\t{1}", distanceString, headingString);
                                    await Task.Delay(500, cts.Token);
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

                            await Task.Delay(2000, cts.Token);
                        }

                        Win32.CloseHandle(dakarProcessHandle);
                    }

                    Console.WriteLine("program is not running");
                    await Task.Delay(2000, cts.Token);
                }
            }, cts.Token);
        }

        public void stop() {
            cts?.Cancel();
            mostRecentDistanceAndHeading = new DistanceAndHeading(0, 0);
            mostRecentWaypoints = 0;
        }

        private static string readStringFromProcessMemory(IntPtr processHandle, ProcessModule module, IEnumerable<int> offsets, int maxCharacters) {
            byte[] stringBuffer = new byte[maxCharacters * 2];

            IntPtr stringAddress = getMemoryAddressByOffsetChain(processHandle, module, offsets);

            bool success = Win32.ReadProcessMemory(processHandle, stringAddress, stringBuffer, stringBuffer.Length, out long bytesRead);
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
                bool success = Win32.ReadProcessMemory(processHandle, memoryAddress, out IntPtr memoryValue, Marshal.SizeOf<long>(), out long bytesRead);
                if (!success) {
                    throw new ApplicationException($"Could not read memory address 0x{memoryAddress.ToInt64():X}: {Marshal.GetLastWin32Error()}");
                }

                memoryAddress = memoryValue;
            }

            return memoryAddress;
        }

    }

}