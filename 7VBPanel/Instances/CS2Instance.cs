using _7VBPanel.Components;
using _7VBPanel.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace _7VBPanel.Instances
{
    public class CS2Instance
    {
        public Process CS2Process;
        private AccountInstance accountInstance;
        public CS2WindowComponent CS2_WindowComponent = new CS2WindowComponent();


        public InputSimulator inputSimulator = new InputSimulator();
        public CS2Instance(AccountInstance accountInstance)
        {
            this.accountInstance = accountInstance;
        }
        public void Setup()
        {
            while (CS2Process == null)
            {
                Thread.Sleep(1000);
            }
            CS2_WindowComponent.Setup(CS2Process.MainWindowHandle);
        }
        public void Stop()
        {
            try
            {
                if (CS2Process != null)
                {
                    CS2Process.Kill();
                }

            }
            catch (Exception ex) { }
        }
        public void SendText(string text)
        {
            const int WM_CHAR = 0x0102;
            foreach (char c in text)
            {
                Win32.SendMessage(CS2Process.MainWindowHandle, WM_CHAR, (IntPtr)c, IntPtr.Zero);
            }
        }
        public void SetForeground()
        {
            Win32.BringWindowToFront(CS2Process.MainWindowHandle);
        }
        public void ClickMouseInWindowCoordinates(int x, int y, int sleepTime = 500)
        {
            const int MOUSEEVENTF_LEFTDOWN = 0x02;
            const int MOUSEEVENTF_LEFTUP = 0x04;

            Win32.SetForegroundWindow(CS2Process.MainWindowHandle);
            MoveMouseToWindowCoordinates(x, y);
            Thread.Sleep(sleepTime);
            Win32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Win32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        public void MoveMouseToWindowCoordinates(int x, int y)
        {
            Point windowPosition = Win32.GetWindowPosition(CS2Process.MainWindowHandle);

            Size borderSize = Win32.GetWindowBorderSize(CS2Process.MainWindowHandle);

            int correctedX = (int)(windowPosition.X + x);
            int correctedY = (int)(windowPosition.Y + y);

            Win32.SetForegroundWindow(CS2Process.MainWindowHandle);
            IntPtr hdc = Win32.GetDC(IntPtr.Zero);

            int screenWidth = Win32.GetDeviceCaps(hdc, Win32.DESKTOPHORZRES);
            int screenHeight = Win32.GetDeviceCaps(hdc, Win32.DESKTOPVERTRES);

            Win32.ReleaseDC(IntPtr.Zero, hdc);

            int ConvertedX = (int)(65535 * (correctedX + 1) / screenWidth);
            int ConvertedY = (int)(65535 * (correctedY + 1) / screenHeight);
            inputSimulator.Mouse.MoveMouseTo(ConvertedX, ConvertedY);
        }
    }

}
