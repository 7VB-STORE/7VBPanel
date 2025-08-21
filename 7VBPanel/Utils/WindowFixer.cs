using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace _7VBPanel.Utils
{
    public static class WindowFixer
    {
        // Определяем структуру RECT
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Добавляем необходимые P/Invoke методы
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public static void ForceSetCS2WindowStyleAndSize(IntPtr cs2WindowHandle, int width = 360, int height = 270, int x = 100, int y = 100, bool withBorders = true)
        {
            if (cs2WindowHandle == IntPtr.Zero)
            {
                Console.WriteLine("❌ Ошибка: пустой хэндл окна CS2.");
                return;
            }

            // Получить текущий стиль окна
            int style = Win32.GetWindowLong(cs2WindowHandle, -16); // GWL_STYLE

            if (withBorders)
            {
                // Добавить рамки и заголовок
                style |= 0x00C00000 | 0x00040000 | 0x00800000 | 0x00020000; // WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_MINIMIZEBOX
                Console.WriteLine("✅ Установлены рамки окна");
            }
            else
            {
                // Убрать рамки и заголовок
                style &= ~(0x00C00000 | 0x00040000 | 0x00800000 | 0x00020000); // WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_MINIMIZEBOX
                Console.WriteLine("❌ Убраны рамки окна");
            }

            Win32.SetWindowLong(cs2WindowHandle, -16, style);

            // Установить новое положение и размер
            Win32.SetWindowPos(
                cs2WindowHandle,
                IntPtr.Zero,
                x, y,
                width, height,
                (uint)(0x0004 | 0x0020 | 0x0040) // SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW
            );

            Console.WriteLine($"✅ CS2 окно установлено: {width}x{height} @ ({x},{y}) с {(withBorders ? "рамками" : "без рамок")}");
        }

        // Метод для запуска нескольких копий CS2 через один Steam
        public static async Task StartMultipleCS2Instances(int count, string steamPath = @"C:\Program Files (x86)\Steam\steam.exe", bool withBorders = true)
        {
            Console.WriteLine($"🚀 Запуск {count} копий CS2 через один Steam с {(withBorders ? "рамками" : "без рамок")}");

            try
            {
                // Сначала запускаем Steam (если еще не запущен)
                if (!IsSteamRunning())
                {
                    Console.WriteLine("🎮 Запуск Steam...");
                    var steamStartInfo = new ProcessStartInfo
                    {
                        FileName = steamPath,
                        Arguments = "-no-browser -silent",
                        UseShellExecute = true
                    };

                    Process.Start(steamStartInfo);
                    Console.WriteLine("✅ Steam запущен, ожидание инициализации...");
                    await Task.Delay(10000); // Ждем инициализацию Steam
                }
                else
                {
                    Console.WriteLine("✅ Steam уже запущен");
                }

                // Запускаем несколько копий CS2 с задержкой
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        Console.WriteLine($"🎮 Запуск CS2 #{i + 1}...");

                        var cs2StartInfo = new ProcessStartInfo
                        {
                            FileName = steamPath,
                            Arguments = $"-applaunch 730 -windowed -nojoy -novid - sw -w 360 -h 270",
                            UseShellExecute = true
                        };

                        Process.Start(cs2StartInfo);
                        Console.WriteLine($"✅ CS2 #{i + 1} запущен");

                        // Ждем между запусками
                        if (i < count - 1)
                        {
                            Console.WriteLine($"⏳ Ожидание перед следующим запуском...");
                            await Task.Delay(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Ошибка запуска CS2 #{i + 1}: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Запущено {count} копий CS2");

                // Ждем немного, чтобы окна появились
                await Task.Delay(8000);

                // Настраиваем все окна
                SetupAllWindows(360, 270, withBorders);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка запуска множественных экземпляров: {ex.Message}");
            }
        }

        // Проверка, запущен ли Steam
        private static bool IsSteamRunning()
        {
            try
            {
                Process[] steamProcesses = Process.GetProcessesByName("steam");
                return steamProcesses.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        // Метод для завершения процессов CS2
        private static void KillCS2Processes()
        {
            try
            {
                Process[] cs2Processes = Process.GetProcessesByName("cs2");
                foreach (Process proc in cs2Processes)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(2000);
                        Console.WriteLine($"✅ Процесс CS2 завершен");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Ошибка завершения CS2: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка получения процессов CS2: {ex.Message}");
            }
        }

        // Метод для настройки окон с рамками
        public static void SetupAllWindows(int width = 360, int height = 270, bool withBorders = true)
        {
            Console.WriteLine($"🔧 Настройка всех окон CS2: {width}x{height} с {(withBorders ? "рамками" : "без рамок")}");

            Win32.EnumWindows((hWnd, lParam) =>
            {
                if (Win32.IsWindowVisible(hWnd))
                {
                    StringBuilder sb = new StringBuilder(256);
                    Win32.GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();

                    if (title.Contains("Counter-Strike 2") || title.Contains("CS2") || title.Contains("Counter-Strike"))
                    {
                        Console.WriteLine($"🔧 Найдено окно CS2: {title}");

                        // Получаем текущую позицию для правильного размещения
                        RECT rect;
                        if (GetWindowRect(hWnd, out rect))
                        {
                            ForceSetCS2WindowStyleAndSize(
                                hWnd,
                                width,
                                height,
                                rect.Left,
                                rect.Top,
                                withBorders
                            );
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        // Метод для закрытия всех CS2
        public static void CloseAllCS2()
        {
            Console.WriteLine("🚪 Закрытие всех экземпляров CS2...");
            KillCS2Processes();
            Console.WriteLine("✅ Все экземпляры CS2 закрыты");
        }
    }
}