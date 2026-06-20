using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace _7VBPanel.Utils
{
    /// <summary>
    /// Поиск зелёной кнопки &quot;ACCEPT&quot; в клиентской области окна (скрин + зелёный кластер).
    /// </summary>
    public static class MatchAcceptButtonFinder
    {
        /// <summary>Типичная паннорамная зелёть кнопки Accept (допуск по каналам).</summary>
        private static bool IsAcceptButtonGreen(int r, int g, int b)
        {
            if (g < 85)
                return false;
            if (g < r + 12)
                return false;
            if (g < b + 12)
                return false;
            if (r > 190 && b > 190)
                return false;
            if (g > 255)
                return false;
            return true;
        }

        private static bool IsAcceptButtonGreenLoose(int r, int g, int b)
        {
            if (g < 70)
                return false;
            if (g < r + 5)
                return false;
            if (g < b + 5)
                return false;
            return g - r > 3 && g - b > 3;
        }

        /// <summary>Ищет центр &quot;пятна&quot; зелёных пикселей в нижней части кадра (кнопка Accept).</summary>
        public static bool TryGetClickClientCoords(IntPtr hWnd, out int clientX, out int clientY)
        {
            clientX = 0;
            clientY = 0;
            if (hWnd == IntPtr.Zero)
                return false;
            if (!Win32.GetClientRect(hWnd, out RECT cr))
                return false;
            int w = cr.Right - cr.Left;
            int h = cr.Bottom - cr.Top;
            if (w < 48 || h < 48)
                return false;

            var p0 = new POINT { X = 0, Y = 0 };
            if (!Win32.ClientToScreen(hWnd, ref p0))
                return false;

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    try
                    {
                        g.CopyFromScreen(p0.X, p0.Y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
                    }
                    catch
                    {
                        return false;
                    }
                }

                int yStart = (int)(h * 0.35);
                if (TryClusterCenter(bmp, w, h, yStart, IsAcceptButtonGreen, out long sx, out long sy, out long n) && n >= 80)
                {
                    clientX = (int)(sx / n);
                    clientY = (int)(sy / n);
                    return true;
                }
                if (TryClusterCenter(bmp, w, h, yStart, IsAcceptButtonGreenLoose, out sx, out sy, out n) && n >= 50)
                {
                    clientX = (int)(sx / n);
                    clientY = (int)(sy / n);
                    return true;
                }
            }

            return false;
        }

        private static bool TryClusterCenter(Bitmap bmp, int w, int h, int y0, Func<int, int, int, bool> isGreen, out long sumX, out long sumY, out long n)
        {
            sumX = 0;
            sumY = 0;
            n = 0;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                IntPtr scan0 = data.Scan0;
                int stride = data.Stride;
                for (int y = y0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int o = y * stride + x * 4;
                        int c = Marshal.ReadInt32(scan0, o);
                        int b = c & 0xFF, g2 = (c >> 8) & 0xFF, r = (c >> 16) & 0xFF;
                        if (!isGreen(r, g2, b))
                            continue;
                        sumX += x;
                        sumY += y;
                        n++;
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return n > 0;
        }
    }
}
