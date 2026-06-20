using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace _7VBPanel.Utils
{
    public static class SideDetector
    {
        /// <summary>
        /// T — жёлтый/оранжевый, CT — голубой/синий (как в dsa_v16_8to8.py).
        /// </summary>
        public static string DetectSide(Bitmap img)
        {
            if (img == null)
                return null;

            int tPixels = 0;
            int ctPixels = 0;

            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int r = c.R;
                    int g = c.G;
                    int b = c.B;

                    if (r > 100 && g > 90 && r > b + 35)
                        tPixels++;
                    if (b > 110 && b > r + 25)
                        ctPixels++;
                }
            }

            PanelLog.Line($"  🔍 Пиксели: T={tPixels}, CT={ctPixels}");

            if (ctPixels < 12 && tPixels < 12)
            {
                PanelLog.Line("  ⚠️ Мало пикселей! Меню не открыто?");
                return null;
            }

            if (ctPixels >= tPixels * 1.35)
                return "CT";
            if (tPixels >= ctPixels * 1.35)
                return "T";

            PanelLog.Line("  ⚠️ Неоднозначные цвета (CT≈T)");
            return null;
        }

        public static Bitmap GrabBuyMenuArea(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Win32.GetWindowRect(hwnd, out RECT rect))
                return null;

            int winW = rect.Right - rect.Left;
            int winH = rect.Bottom - rect.Top;
            if (winW <= 0 || winH <= 0)
                return null;

            int x1 = rect.Left + (int)(winW * 0.05);
            int y1 = rect.Top + (int)(winH * 0.15);
            int x2 = rect.Left + (int)(winW * 0.45);
            int y2 = rect.Top + (int)(winH * 0.85);

            int w = Math.Max(1, x2 - x1);
            int h = Math.Max(1, y2 - y1);

            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(x1, y1, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }
    }
}
