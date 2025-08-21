using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace _7VBPanel.Utils
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public enum MonitorDpiType
    {
        EffectiveDpi = 0,
        AngularDpi = 1,
        RawDpi = 2,
    }

    public static class Win32
    {
        // Константы стилей окон
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_BORDER = 0x00800000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;

        // Флаги SetWindowPos
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_DPISCALED = 0x2000;
        public const uint SWP_ASYNCWINDOWPOS = 0x4000;

        // Другие константы
        public const int DESKTOPHORZRES = 118;
        public const int DESKTOPVERTRES = 117;

        private const uint SMTO_ABORTIFHUNG = 0x0002;
        private const uint WM_NULL = 0x0000;

        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_EXTENDED_FRAME_BOUNDS = 9
        }

        public enum EShowWindow
        {
            Hide = 0,
            ShowNormal = 1,
            ShowMinimized = 2,
            Maximize = 3,
            ShowNormalNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7,
            ShowNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimized = 11
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // ==================== DLL IMPORTS ====================

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, EShowWindow flags);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsHungAppWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsProc lpfn, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);


        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool EnableNonClientDpiScaling(IntPtr hWnd);

        // ==================== ДОБАВЛЕННЫЕ МЕТОДЫ ====================

        /// <summary>
        /// Принудительно устанавливает размер окна с удалением рамок
        /// </summary>
        public static bool ForceWindowSize(IntPtr hWnd, int width, int height)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            try
            {
                // Включаем DPI-осознание
                EnableNonClientDpiScaling(hWnd);

                // Удаляем стандартные рамки
                int style = GetWindowLong(hWnd, GWL_STYLE);
                int newStyle = style & ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                SetWindowLong(hWnd, GWL_STYLE, newStyle);

                // Рассчитываем размер с учетом границ
                RECT rect = new RECT { Right = width, Bottom = height };
                if (!AdjustWindowRectEx(ref rect, (uint)newStyle, false, 0))
                {
                    Debug.WriteLine("AdjustWindowRectEx failed");
                    return false;
                }

                // Применяем новый размер
                bool success = SetWindowPos(
                    hWnd,
                    IntPtr.Zero,
                    0, 0,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top,
                    SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_ASYNCWINDOWPOS
                );

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"SetWindowPos error: {error} - {new Win32Exception(error).Message}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ForceWindowSize error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Активирует и восстанавливает окно
        /// </summary>
        public static void BringWindowToFront(IntPtr hWnd)
        {
            ShowWindow(hWnd, (int)EShowWindow.Restore);
            SetForegroundWindow(hWnd);
        }

        /// <summary>
        /// Получает позицию окна (с учетом DWM)
        /// </summary>
        public static Point GetWindowPosition(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return Point.Empty;

            try
            {
                RECT rect;
                int rectSize = Marshal.SizeOf(typeof(RECT));

                // Пытаемся получить точные координаты через DWM (Windows Vista+)
                if (Environment.OSVersion.Version.Major >= 6 &&
                    DwmGetWindowAttribute(hWnd, (int)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                                        out rect, rectSize) == 0)
                {
                    return new Point(rect.Left, rect.Top);
                }

                // Fallback: стандартный способ через GetWindowRect
                if (GetWindowRect(hWnd, out rect))
                {
                    return new Point(rect.Left, rect.Top);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения позиции окна: {ex.Message}");
            }

            return Point.Empty;
        }

        /// <summary>
        /// Получает размер границ окна
        /// </summary>
        public static Size GetWindowBorderSize(IntPtr hWnd)
        {
            RECT rect = new RECT();
            uint style = (uint)GetWindowLong(hWnd, GWL_STYLE);
            uint exStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);

            if (AdjustWindowRectEx(ref rect, style, false, exStyle))
            {
                return new Size(-rect.Left, -rect.Top);
            }
            throw new InvalidOperationException("Unable to get window border size.");
        }

        /// <summary>
        /// Получает размер клиентской области окна
        /// </summary>
        public static Size GetClientSize(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return Size.Empty;

            if (GetClientRect(hWnd, out RECT rect))
            {
                return new Size(rect.Right, rect.Bottom);
            }
            return Size.Empty;
        }

        /// <summary>
        /// Устанавливает точный размер клиентской области
        /// </summary>
        public static bool SetExactClientSize(IntPtr hWnd, int clientWidth = 360, int clientHeight = 270)
        {
            if (hWnd == IntPtr.Zero) return false;

            int style = GetWindowLong(hWnd, GWL_STYLE);
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

            // Убираем рамки
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER);
            SetWindowLong(hWnd, GWL_STYLE, style);

            RECT rect = new RECT
            {
                Left = 0,
                Top = 0,
                Right = clientWidth,
                Bottom = clientHeight
            };

            if (!AdjustWindowRectEx(ref rect, (uint)style, false, (uint)exStyle))
                return false;

            int totalWidth = rect.Right - rect.Left;
            int totalHeight = rect.Bottom - rect.Top;

            return SetWindowPos(
                hWnd,
                IntPtr.Zero,
                0, 0,
                totalWidth, totalHeight,
                SWP_NOZORDER | SWP_FRAMECHANGED | SWP_NOMOVE
            );
        }
    }
}