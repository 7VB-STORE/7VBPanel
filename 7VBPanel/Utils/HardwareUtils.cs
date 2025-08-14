using _7VBPanel.Managers;
using _7VBPanel.Structures;
//using LibreHardwareMonitor.Hardware;
//using LibreHardwareMonitor.Hardware.Cpu;
using Microsoft.VisualBasic.Devices;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using Computer = LibreHardwareMonitor.Hardware.Computer;

namespace _7VBPanel.Utils
{
    public static class HardwareUtils
    {
        public static void InitializeHardwareMonitor()
        {

        }
        public static void GetVendorAndDeviceID(out int vendorID, out int deviceID)
        {
            vendorID = 0;
            deviceID = 0;
            using (var factory = new Factory1())
            {
                Adapter highPerformanceAdapter = null;
                long maxVRAM = 0;

                foreach (var adapter in factory.Adapters)
                {
                    var description = adapter.Description;

                    long adapterVRAM = description.DedicatedVideoMemory;

                    if (adapterVRAM > maxVRAM)
                    {
                        maxVRAM = adapterVRAM;
                        highPerformanceAdapter = adapter;
                    }

                }

                if (highPerformanceAdapter != null)
                {
                    var desc = highPerformanceAdapter.Description;

                    vendorID = desc.VendorId;
                    deviceID = desc.DeviceId;
                }
            }
        }
    }

}
