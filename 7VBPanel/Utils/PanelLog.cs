using System;
using System.IO;
using System.Text;

namespace _7VBPanel.Utils
{
    /// <summary>Лог в файл и в консоль (если она открыта) — WPF WinExe не всегда пишет в <see cref="Console"/>.</summary>
    public static class PanelLog
    {
        private static readonly object LockObj = new object();
        private static string LogFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7VBPanel.log");

        public static void Line(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture) + " " + message;
            lock (LockObj)
            {
                try
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                }
                try
                {
                    Console.WriteLine(message);
                }
                catch
                {
                }
            }
            try
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
            catch
            {
            }
        }
    }
}
