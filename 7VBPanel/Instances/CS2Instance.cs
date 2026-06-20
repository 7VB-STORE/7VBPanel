using _7VBPanel.Components;
using _7VBPanel.Managers;
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
            while (CS2Process == null || CS2Process.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(500);
            }

            CS2_WindowComponent.Setup(CS2Process.MainWindowHandle);

            // Принудительно устанавливаем размер окна
            Task.Run(() =>
            {
                SettingsManager.GetCs2WindowClientSize(out int w, out int h);
                int attempts = 0;
                while (attempts < 10)
                {
                    if (Win32.ForceWindowSize(CS2Process.MainWindowHandle, w, h))
                    {
                        Console.WriteLine($"✅ Размер окна изменён на {w}×{h} (как в CS2Arguments -w/-h)");
                        break;
                    }
                    attempts++;
                    Thread.Sleep(500);
                }

                if (attempts >= 10)
                {
                    Console.WriteLine("❌ Не удалось изменить размер окна");
                }
            });
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
            if (CS2Process == null || CS2Process.MainWindowHandle == IntPtr.Zero)
                return;
            ClickMouseInClient(CS2Process.MainWindowHandle, x, y, sleepTime);
        }

        /// <summary>Клик в клиентских координатах указанного окна (то же окно, что при поиске кнопки Accept).</summary>
        public void ClickMouseInClient(IntPtr clientHwnd, int x, int y, int sleepTime = 500)
        {
            const int MOUSEEVENTF_LEFTDOWN = 0x02;
            const int MOUSEEVENTF_LEFTUP = 0x04;
            if (clientHwnd == IntPtr.Zero)
                return;
            var pt = new POINT { X = x, Y = y };
            if (!Win32.ClientToScreen(clientHwnd, ref pt))
                return;
            Win32.SetForegroundWindow(clientHwnd);
            Thread.Sleep(40);
            Win32.SetCursorPos(pt.X, pt.Y);
            Thread.Sleep(sleepTime);
            Win32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Win32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <param name="autoAcceptGroupWave">Если true (волна AutoAccept) — кликать в каждом окне; иначе отмена при pe==done после приёма.</param>
        public void TryClickMatchAcceptButton(bool autoAcceptGroupWave = false)
        {
            const int acceptLiftPx = 10;
            if (CS2Process == null)
                return;
            if (!autoAcceptGroupWave && accountInstance != null)
            {
                string pe = accountInstance.LastMatchIdCompact;
                string done = accountInstance.LastAcceptedMatchIdCompact;
                if (!string.IsNullOrEmpty(done) && !string.IsNullOrEmpty(pe) && pe == done)
                {
                    PanelLog.Line($"[AutoAccept] {accountInstance.Login} клик отменён: в логе снова тот же id={done}, он уже был принят");
                    return;
                }
            }
            try
            {
                CS2Process.Refresh();
            }
            catch
            {
            }
            IntPtr h = Win32.FindLargestTopLevelWindowForProcessId(CS2Process.Id);
            if (h == IntPtr.Zero)
                h = CS2Process.MainWindowHandle;
            if (h == IntPtr.Zero)
                return;

            Thread.Sleep(1000);

            if (MatchAcceptButtonFinder.TryGetClickClientCoords(h, out int cx, out int cy))
            {
                cy = Math.Max(1, cy - acceptLiftPx);
                string who = accountInstance?.Login ?? "?";
                PanelLog.Line($"[AutoAccept] {who} ACCEPT по цвету → ({cx},{cy}) [Y−{acceptLiftPx}px]");
                ClickMouseInClient(h, cx, cy, 280);
                return;
            }
            SettingsManager.GetCs2WindowClientSize(out int cw, out int ch);
            if (cw <= 0) cw = 360;
            if (ch <= 0) ch = 270;
            int fx = Math.Max(1, cw / 2);
            int fy = Math.Max(1, (int)(ch * 0.80) - acceptLiftPx);
            string who2 = accountInstance?.Login ?? "?";
            PanelLog.Line($"[AutoAccept] {who2} кнопка не распознана, fallback ({fx},{fy})");
            ClickMouseInClient(h, fx, fy, 280);
        }

        public void MoveMouseToWindowCoordinates(int x, int y)
        {
            if (CS2Process == null || CS2Process.MainWindowHandle == IntPtr.Zero)
                return;

            // (x, y) — в координатах клиентской области окна; раньше складывали с DWM-верхом без non-client смещения — курсор уезжал
            var pt = new POINT { X = x, Y = y };
            if (!Win32.ClientToScreen(CS2Process.MainWindowHandle, ref pt))
                return;

            Win32.SetForegroundWindow(CS2Process.MainWindowHandle);
            Thread.Sleep(30);
            Win32.SetCursorPos(pt.X, pt.Y);
        }

    }

}
