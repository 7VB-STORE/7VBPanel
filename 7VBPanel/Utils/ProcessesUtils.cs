using _7VBPanel.Instances;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _7VBPanel.Utils
{
    public static class ProcessesUtils
    {

        public static Process[] GetProcessesByParentPID(int parentPID, string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return Array.FindAll(processes, process => IsChildProcess(process, parentPID));
        }

        public static bool IsChildProcess(Process process, int parentPID)
        {
            try
            {
                while (process != null)
                {
                    if (process.Id == parentPID)
                    {
                        return true;
                    }
                    process = GetParentProcess(process);
                }
            }
            catch (Exception)
            {
                // Handle exception
            }

            return false;
        }
        public static HashSet<string> GetLoadedDlls(int pid)
        {
            var dlls = new HashSet<string>();
            try
            {
                Process process = Process.GetProcessById(pid);
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.ModuleName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        dlls.Add(module.ModuleName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing process: {ex.Message}");
            }
            return dlls;
        }

        public static Process GetParentProcess(Process process)
        {
            try
            {
                using (ManagementObject mo = new ManagementObject($"win32_process.handle='{process.Id}'"))
                {
                    mo.Get();
                    int parentPID = Convert.ToInt32(mo["ParentProcessId"]);
                    return Process.GetProcessById(parentPID);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void KillAllSteamAndCS()
        {
            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                try
                {
                    if (process.ProcessName.ToLower().Contains("steam"))
                    {
                        process.Kill();
                    }
                    if (process.ProcessName.ToLower().Contains("cs2"))
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
