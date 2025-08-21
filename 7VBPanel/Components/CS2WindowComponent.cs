using System;
using _7VBPanel.Utils;
using System.Drawing;
using System.Runtime.InteropServices;
using static _7VBPanel.Utils.Win32;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace _7VBPanel.Components
{
    public class CS2WindowComponent : IDisposable
    {
        public IntPtr GetWindowHandle()
        {
            if (_cs2Window == IntPtr.Zero)
                throw new InvalidOperationException("Window handle is not initialized");

            return _cs2Window;
        }

        public class WindowConfig
        {
            public int Width { get; set; } = 360;
            public int Height { get; set; } = 270;
            public bool WithBorders { get; set; } = true;
            public Point Position { get; set; } = Point.Empty;
            public string Title { get; set; } = "Counter-Strike 2";
            public bool SizeLockEnabled { get; set; } = false;
            public int MonitoringIntervalMs { get; set; } = 100;
            public bool EnableSound { get; set; } = true; // Новая настройка

            public WindowConfig Clone()
            {
                return new WindowConfig
                {
                    Width = this.Width,
                    Height = this.Height,
                    WithBorders = this.WithBorders,
                    Position = this.Position,
                    Title = this.Title,
                    SizeLockEnabled = this.SizeLockEnabled,
                    MonitoringIntervalMs = this.MonitoringIntervalMs,
                    EnableSound = this.EnableSound
                };
            }
        }

        private IntPtr _cs2Window;
        private WindowConfig _currentConfig;
        private static readonly StringBuilder _sharedStringBuilder = new StringBuilder(256);
        private Timer _sizeCheckTimer;
        private static readonly List<CS2Instance> _runningInstances = new List<CS2Instance>();
        private bool _disposed = false;

        public IntPtr WindowHandle => _cs2Window;
        public WindowConfig CurrentConfig => _currentConfig;

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        static CS2WindowComponent()
        {
            SetProcessDPIAware();
        }

        public CS2WindowComponent(WindowConfig config = null)
        {
            _currentConfig = config?.Clone() ?? new WindowConfig();
            ValidateConfig(_currentConfig);
        }

        private void ValidateConfig(WindowConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (config.Width <= 0 || config.Height <= 0)
                throw new ArgumentOutOfRangeException("Window dimensions must be positive");

            if (config.MonitoringIntervalMs < 50)
                throw new ArgumentOutOfRangeException("Monitoring interval must be at least 50ms");
        }

        public void Setup(IntPtr cs2Window)
        {
            if (cs2Window == IntPtr.Zero)
                throw new ArgumentException("Invalid window handle");

            _cs2Window = cs2Window;
            ApplyConfig();
        }

        public void UpdateConfig(WindowConfig newConfig)
        {
            if (newConfig == null)
                throw new ArgumentNullException(nameof(newConfig));

            ValidateConfig(newConfig);
            _currentConfig = newConfig.Clone();

            if (_cs2Window != IntPtr.Zero)
                ApplyConfig();
        }

        public void ApplyConfig()
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentConfig.Title))
                    ChangeWindowTitle(_currentConfig.Title);

                SetWindowSize(
                    _currentConfig.Position.X,
                    _currentConfig.Position.Y,
                    _currentConfig.Width,
                    _currentConfig.Height,
                    _currentConfig.WithBorders
                );

                if (_currentConfig.SizeLockEnabled)
                    EnableSizeLock();
                else
                    DisableSizeLock();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error applying config: {ex.Message}");
                throw;
            }
        }

        public void MoveCSWindow(int x, int y)
        {
            if (_cs2Window == IntPtr.Zero)
                throw new InvalidOperationException("Window not initialized");

            try
            {
                bool result = SetWindowPos(
                    _cs2Window,
                    IntPtr.Zero,
                    x,
                    y,
                    0,
                    0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_DPISCALED
                );

                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, $"Failed to move window to ({x},{y})");
                }

                _currentConfig.Position = new Point(x, y);
                Console.WriteLine($"✅ Window moved to: {x}x{y}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MoveCSWindow error: {ex.Message}");
                throw;
            }
        }

        public void ChangeWindowTitle(string title)
        {
            if (_cs2Window == IntPtr.Zero)
                throw new InvalidOperationException("Window not initialized");

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty");

            SetWindowText(_cs2Window, title);
        }

        public void SetWindowSize(int x, int y, int width, int height, bool withBorders)
        {
            if (_cs2Window == IntPtr.Zero)
                throw new InvalidOperationException("Window not initialized");

            try
            {
                int style = GetWindowLong(_cs2Window, GWL_STYLE);
                style = withBorders
                    ? style | WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_MINIMIZEBOX
                    : style & ~(WS_CAPTION | WS_THICKFRAME | WS_MAXIMIZEBOX | WS_MINIMIZEBOX | WS_BORDER);

                SetWindowLong(_cs2Window, GWL_STYLE, style);

                if (SetExactClientSize(_cs2Window, width, height))
                {
                    SetWindowPos(_cs2Window, IntPtr.Zero, x, y, 0, 0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                    _currentConfig.Position = new Point(x, y);
                    Console.WriteLine($"✅ Window set to: {width}x{height} ({(withBorders ? "with" : "without")} borders)");
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SetWindowSize error: {ex.Message}");
                throw;
            }
        }

        public void EnableSizeLock()
        {
            if (_cs2Window == IntPtr.Zero)
                throw new InvalidOperationException("Window not initialized");

            _currentConfig.SizeLockEnabled = true;
            _sizeCheckTimer?.Dispose();
            _sizeCheckTimer = new Timer(SizeCheckCallback, null, 0, _currentConfig.MonitoringIntervalMs);

            Console.WriteLine($"✅ Size lock enabled for {_currentConfig.Width}x{_currentConfig.Height}");
        }

        public void DisableSizeLock()
        {
            if (_cs2Window == IntPtr.Zero)
                throw new InvalidOperationException("Window not initialized");

            _currentConfig.SizeLockEnabled = false;
            _sizeCheckTimer?.Dispose();
            _sizeCheckTimer = null;

            Console.WriteLine("🔓 Size lock disabled");
        }

        private void SizeCheckCallback(object state)
        {
            if (!_currentConfig.SizeLockEnabled || _cs2Window == IntPtr.Zero)
                return;

            try
            {
                if (IsWindowVisible(_cs2Window) && IsWindow(_cs2Window))
                {
                    var currentSize = Win32.GetClientSize(_cs2Window);
                    if (currentSize.Width != _currentConfig.Width || currentSize.Height != _currentConfig.Height)
                    {
                        SetWindowSize(
                            _currentConfig.Position.X,
                            _currentConfig.Position.Y,
                            _currentConfig.Width,
                            _currentConfig.Height,
                            _currentConfig.WithBorders
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Size monitoring error: {ex.Message}");
            }
        }

        public static async Task<List<CS2Instance>> LaunchSequentialSteamInstances(
            List<WindowConfig> configs,
            string steamPath = @"C:\Program Files (x86)\Steam\steam.exe")
        {
            if (configs == null || !configs.Any())
                throw new ArgumentException("At least one window config required");

            if (!File.Exists(steamPath))
                throw new FileNotFoundException("Steam executable not found", steamPath);

            Console.WriteLine($"🚀 Launching {configs.Count} Steam instances...");
            var instances = new List<CS2Instance>();

            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                Dictionary<string, string> originalEnv = null;

                try
                {
                    string profilePath = Path.Combine(Environment.CurrentDirectory, $"SteamProfile_{i}");
                    CreateProfileDirectories(profilePath);

                    if (i > 0)
                    {
                        Console.WriteLine($"⏳ Killing previous Steam processes...");
                        KillSteamProcesses();
                        await Task.Delay(3000);
                    }

                    originalEnv = SaveEnvironmentVariables();
                    SetEnvironmentVariablesForProfile(profilePath);

                    Console.WriteLine($"🎮 Starting Steam #{i + 1} with profile: {profilePath}");
                    var steamProcess = StartSteamProcess(steamPath);
                    if (steamProcess == null) continue;

                    var instance = new CS2Instance
                    {
                        InstanceId = i,
                        Process = steamProcess,
                        ProfilePath = profilePath,
                        Config = config.Clone()
                    };

                    instances.Add(instance);
                    _runningInstances.Add(instance);

                    Console.WriteLine($"✅ Steam #{i + 1} started, waiting for initialization...");
                    await Task.Delay(12000); // Ждем больше времени для инициализации

                    Console.WriteLine($"🎮 Starting CS2 for Steam #{i + 1}...");
                    var cs2Process = StartCS2Process(steamPath, config.Width, config.Height, config.EnableSound);
                    if (cs2Process != null)
                    {
                        instance.CS2Process = cs2Process;
                        Console.WriteLine($"✅ CS2 #{i + 1} started");
                    }

                    RestoreEnvironmentVariables(originalEnv);

                    if (i < configs.Count - 1)
                    {
                        Console.WriteLine($"⏳ Waiting before next instance...");
                        await Task.Delay(15000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error launching instance {i}: {ex.Message}");
                    if (originalEnv != null)
                        RestoreEnvironmentVariables(originalEnv);
                }
            }

            Console.WriteLine($"✅ All {instances.Count} instances launched, starting window monitoring...");
            _ = Task.Run(async () => await MonitorAndSetupWindows(configs));
            return instances;
        }

        private static Dictionary<string, string> SaveEnvironmentVariables()
        {
            return new Dictionary<string, string>
            {
                ["USERPROFILE"] = Environment.GetEnvironmentVariable("USERPROFILE"),
                ["APPDATA"] = Environment.GetEnvironmentVariable("APPDATA"),
                ["LOCALAPPDATA"] = Environment.GetEnvironmentVariable("LOCALAPPDATA")
            };
        }

        private static void SetEnvironmentVariablesForProfile(string profilePath)
        {
            Environment.SetEnvironmentVariable("USERPROFILE", profilePath);
            Environment.SetEnvironmentVariable("APPDATA", Path.Combine(profilePath, "AppData", "Roaming"));
            Environment.SetEnvironmentVariable("LOCALAPPDATA", Path.Combine(profilePath, "AppData", "Local"));
            Environment.SetEnvironmentVariable("TEMP", Path.Combine(profilePath, "AppData", "Local", "Temp"));
            Environment.SetEnvironmentVariable("TMP", Path.Combine(profilePath, "AppData", "Local", "Temp"));
        }

        private static void RestoreEnvironmentVariables(Dictionary<string, string> originalVars)
        {
            if (originalVars == null) return;

            foreach (var kv in originalVars)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
            }
        }

        private static Process StartSteamProcess(string steamPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = steamPath,
                    Arguments = "-no-browser -silent -multirun -norepair -noverifyfiles -console",
                    UseShellExecute = true
                };

                var process = Process.Start(startInfo);
                Console.WriteLine($"✅ Steam process started with PID: {process?.Id}");
                return process;
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine($"❌ Failed to start Steam: {ex.Message}");
                return null;
            }
        }

        private static Process StartCS2Process(string steamPath, int width, int height, bool enableSound)
        {
            try
            {
                // Формируем аргументы запуска CS2
                var args = new List<string>
                {
                    "-applaunch",
                    "730",
                    "-windowed",
                    $"-w {width}",
                    $"-h {height}",
                    "-nojoy",
                    "-novid"
                };

                // Добавляем -nosound только если звук отключен
                if (!enableSound)
                {
                    args.Add("-nosound");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = steamPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = true
                };

                Console.WriteLine($"🎮 CS2 launch args: {startInfo.Arguments}");
                var process = Process.Start(startInfo);
                return process;
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine($"❌ Failed to start CS2: {ex.Message}");
                return null;
            }
        }

        private static void CreateProfileDirectories(string profilePath)
        {
            string[] directories = {
                profilePath,
                Path.Combine(profilePath, "AppData", "Roaming"),
                Path.Combine(profilePath, "AppData", "Local"),
                Path.Combine(profilePath, "AppData", "Local", "Steam", "htmlcache"),
                Path.Combine(profilePath, "AppData", "Local", "Temp")
            };

            foreach (string dir in directories)
            {
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        Console.WriteLine($"✅ Created directory: {dir}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error creating directory {dir}: {ex.Message}");
                }
            }
        }

        private static void KillSteamProcesses()
        {
            string[] processes = { "steam", "steamservice", "steamwebhelper", "cs2" };

            foreach (string processName in processes)
            {
                try
                {
                    var procs = Process.GetProcessesByName(processName);
                    foreach (var proc in procs)
                    {
                        try
                        {
                            Console.WriteLine($"🔫 Killing process {processName} (PID: {proc.Id})");
                            proc.Kill();
                            if (!proc.WaitForExit(5000)) // Ждем 5 секунд
                            {
                                Console.WriteLine($"⚠️ Process {processName} didn't exit gracefully");
                            }
                            else
                            {
                                Console.WriteLine($"✅ Process {processName} killed successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Failed to kill process {processName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error getting processes {processName}: {ex.Message}");
                }
            }
        }

        private static async Task MonitorAndSetupWindows(List<WindowConfig> configs)
        {
            Console.WriteLine("🔍 Starting CS2 windows monitoring...");

            int maxAttempts = 60; // Увеличено до 60 попыток (5 минут)
            int attempt = 0;
            var processedWindows = new List<IntPtr>();

            while (attempt < maxAttempts && _runningInstances.Count > 0)
            {
                attempt++;
                Console.WriteLine($"🔍 Monitoring attempt #{attempt}/60...");
                await Task.Delay(5000);

                try
                {
                    var foundWindows = new List<IntPtr>();
                    EnumWindows((hWnd, lParam) =>
                    {
                        if (IsWindowVisible(hWnd))
                        {
                            _sharedStringBuilder.Clear();
                            int length = GetWindowText(hWnd, _sharedStringBuilder, _sharedStringBuilder.Capacity);
                            string windowTitle = _sharedStringBuilder.ToString();

                            if (length > 0 && (windowTitle.Contains("Counter-Strike 2") ||
                                             windowTitle.Contains("CS2") ||
                                             windowTitle.Contains("Counter-Strike")))
                            {
                                if (!processedWindows.Contains(hWnd))
                                {
                                    foundWindows.Add(hWnd);
                                    Console.WriteLine($"🔍 Found CS2 window: '{windowTitle}'");
                                }
                            }
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (foundWindows.Count > 0)
                    {
                        Console.WriteLine($"🔍 Found {foundWindows.Count} new CS2 windows");

                        foreach (var window in foundWindows)
                        {
                            // Ищем экземпляр без окна
                            var targetInstance = _runningInstances.FirstOrDefault(i => i.WindowHandle == IntPtr.Zero);
                            if (targetInstance != null && !processedWindows.Contains(window))
                            {
                                try
                                {
                                    var component = new CS2WindowComponent(targetInstance.Config);
                                    component.Setup(window);
                                    component.ApplyConfig();

                                    targetInstance.WindowHandle = window;
                                    targetInstance.Component = component;
                                    processedWindows.Add(window);

                                    Console.WriteLine($"✅ Window configured for instance {targetInstance.InstanceId} at position ({targetInstance.Config.Position.X}, {targetInstance.Config.Position.Y})");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"❌ Error configuring window for instance {targetInstance.InstanceId}: {ex.Message}");
                                }
                            }
                        }
                    }

                    // Проверяем, все ли окна настроены
                    int configuredCount = _runningInstances.Count(i => i.WindowHandle != IntPtr.Zero);
                    Console.WriteLine($"📊 Progress: {configuredCount}/{_runningInstances.Count} windows configured");

                    if (_runningInstances.All(i => i.WindowHandle != IntPtr.Zero))
                    {
                        Console.WriteLine($"✅ All {_runningInstances.Count} windows configured successfully!");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Monitoring error: {ex.Message}");
                }
            }

            Console.WriteLine("🔍 Window monitoring completed");
        }

        public static void CloseAllInstances()
        {
            Console.WriteLine("🚪 Closing all CS2 instances...");

            try
            {
                // Закрываем все окна через компоненты
                foreach (var instance in _runningInstances.ToList())
                {
                    try
                    {
                        instance.Component?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error disposing component for instance {instance.InstanceId}: {ex.Message}");
                    }
                }

                // Завершаем процессы
                KillSteamProcesses();

                _runningInstances.Clear();
                Console.WriteLine("✅ All instances closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error closing instances: {ex.Message}");
            }
        }

        public static List<CS2Instance> GetRunningInstances()
        {
            return new List<CS2Instance>(_runningInstances);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sizeCheckTimer?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    public class CS2Instance
    {
        public int InstanceId { get; set; }
        public Process Process { get; set; }
        public Process CS2Process { get; set; }
        public string ProfilePath { get; set; }
        public IntPtr WindowHandle { get; set; }
        public CS2WindowComponent.WindowConfig Config { get; set; }
        public CS2WindowComponent Component { get; set; }

        public bool IsRunning => Process != null && !Process.HasExited;
        public bool HasWindow => WindowHandle != IntPtr.Zero;
    }
}