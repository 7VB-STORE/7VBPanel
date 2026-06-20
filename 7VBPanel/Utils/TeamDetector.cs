using _7VBPanel.Instances;
using _7VBPanel.Managers;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace _7VBPanel.Utils
{
    /// <summary>
    /// Определение CT/T: HUD (иконка сверху) → scoreboard (Tab) → buy-меню (B).
    /// GSI и con_log обрабатываются отдельно в AccountInstance.
    /// </summary>
    public static class TeamDetector
    {
        public static string DetectForAccount(AccountInstance acc, IntPtr hwnd, InputSimulator sim)
        {
            if (acc == null || hwnd == IntPtr.Zero)
                return null;

            string cached = acc.GetFarmTeamIfFresh(TimeSpan.FromSeconds(45));
            if (!string.IsNullOrEmpty(cached))
            {
                PanelLog.Line($"[Farm]   ✅ {acc.Login}: Сторона = {cached} (кэш/{acc.FarmTeamSource})");
                return cached;
            }

            string gsi = GsiListener.TryGetTeam(acc.Login);
            if (!string.IsNullOrEmpty(gsi))
            {
                acc.SetFarmTeam(gsi, "GSI");
                PanelLog.Line($"[Farm]   ✅ {acc.Login}: Сторона = {gsi} (GSI)");
                return gsi;
            }

            string hud = DetectFromHudTeamIcon(hwnd);
            if (!string.IsNullOrEmpty(hud))
            {
                acc.SetFarmTeam(hud, "HUD");
                PanelLog.Line($"[Farm]   ✅ {acc.Login}: Сторона = {hud} (HUD)");
                return hud;
            }

            string scoreboard = DetectFromScoreboard(hwnd, sim);
            if (!string.IsNullOrEmpty(scoreboard))
            {
                acc.SetFarmTeam(scoreboard, "Tab");
                PanelLog.Line($"[Farm]   ✅ {acc.Login}: Сторона = {scoreboard} (Tab)");
                return scoreboard;
            }

            if (sim != null)
            {
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_B);
                Thread.Sleep(700);
            }

            string buy;
            using (var bmp = SideDetector.GrabBuyMenuArea(hwnd))
                buy = SideDetector.DetectSide(bmp);

            if (sim != null)
            {
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_B);
                Thread.Sleep(300);
            }

            if (!string.IsNullOrEmpty(buy))
            {
                acc.SetFarmTeam(buy, "BuyMenu");
                PanelLog.Line($"[Farm]   ✅ {acc.Login}: Сторона = {buy} (BuyMenu)");
                return buy;
            }

            PanelLog.Line($"[Farm]   ❌ {acc.Login}: сторона не определена");
            return null;
        }

        /// <summary>Иконка команды вверху по центру (фризтайм).</summary>
        public static string DetectFromHudTeamIcon(IntPtr hwnd)
        {
            using (var bmp = GrabRegion(hwnd, 0.35, 0.0, 0.65, 0.12))
                return CompareTeamColors(bmp, "HUD");
        }

        /// <summary>Scoreboard: полоса своей строки по центру экрана.</summary>
        public static string DetectFromScoreboard(IntPtr hwnd, InputSimulator sim)
        {
            if (sim == null)
                return null;

            sim.Keyboard.KeyDown(VirtualKeyCode.TAB);
            Thread.Sleep(450);
            try
            {
                using (var row = GrabRegion(hwnd, 0.08, 0.42, 0.92, 0.58))
                {
                    string rowSide = CompareTeamColors(row, "TabRow");
                    if (!string.IsNullOrEmpty(rowSide))
                        return rowSide;
                }

                using (var left = GrabRegion(hwnd, 0.04, 0.25, 0.46, 0.75))
                using (var right = GrabRegion(hwnd, 0.54, 0.25, 0.96, 0.75))
                {
                    CountTeamPixels(left, out int lCt, out int lT);
                    CountTeamPixels(right, out int rCt, out int rT);
                    PanelLog.Line($"  🔍 Tab колонки: L(CT={lCt},T={lT}) R(CT={rCt},T={rT})");
                    if (lCt > lT * 2 && lCt > rCt)
                        return "CT";
                    if (rT > rCt * 2 && rT > lT)
                        return "T";
                }
            }
            finally
            {
                sim.Keyboard.KeyUp(VirtualKeyCode.TAB);
                Thread.Sleep(120);
            }

            return null;
        }

        private static string CompareTeamColors(Bitmap img, string tag)
        {
            if (img == null)
                return null;

            CountTeamPixels(img, out int ct, out int t);
            PanelLog.Line($"  🔍 {tag}: CT={ct}, T={t}");

            if (ct < 12 && t < 12)
                return null;

            if (ct >= t * 1.35)
                return "CT";
            if (t >= ct * 1.35)
                return "T";

            return null;
        }

        private static void CountTeamPixels(Bitmap img, out int ctPixels, out int tPixels)
        {
            ctPixels = 0;
            tPixels = 0;
            if (img == null)
                return;

            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int r = c.R;
                    int g = c.G;
                    int b = c.B;

                    // CT: синий/голубой доминирует
                    if (b > 95 && b > r + 18 && b >= g - 20)
                        ctPixels++;

                    // T: жёлтый/оранжевый (R+G высокие, B низкий)
                    if (r > 95 && g > 75 && r > b + 28 && g > b + 10)
                        tPixels++;
                }
            }
        }

        public static Bitmap GrabRegion(IntPtr hwnd, double x0, double y0, double x1, double y1)
        {
            if (hwnd == IntPtr.Zero || !Win32.GetWindowRect(hwnd, out RECT rect))
                return null;

            int winW = rect.Right - rect.Left;
            int winH = rect.Bottom - rect.Top;
            if (winW <= 0 || winH <= 0)
                return null;

            int left = rect.Left + (int)(winW * x0);
            int top = rect.Top + (int)(winH * y0);
            int right = rect.Left + (int)(winW * x1);
            int bottom = rect.Top + (int)(winH * y1);

            int w = Math.Max(1, right - left);
            int h = Math.Max(1, bottom - top);

            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(left, top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }
    }
}
