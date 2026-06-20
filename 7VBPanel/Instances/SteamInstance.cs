using _7VBPanel.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading;
using _7VBPanel.Utils;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.Core;
using FlaUI.UIA3;

namespace _7VBPanel.Instances
{
    public class SteamInstance
    {
        private AccountInstance accountInstance;
        public Process SteamProcess;
        public SteamInstance(AccountInstance _accountInstance)
        {
            accountInstance = _accountInstance;
        }
        public void Start()
        {
            accountInstance.SetAccountColor(Brushes.Yellow);
            
            // Создаём изолированную папку для аккаунта
            string baseProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PanelData", accountInstance.Login);
            string localPath = Path.Combine(baseProfilePath, "AppData", "Local");
            string localLowPath = Path.Combine(baseProfilePath, "AppData", "LocalLow");
            
            // Очищаем и создаём папки
            if (Directory.Exists(localPath))
            {
                try { Directory.Delete(localPath, true); } catch { }
            }
            Directory.CreateDirectory(localPath);
            Directory.CreateDirectory(localLowPath);
            
            // Создаём символическую ссылку на NVIDIA (требуются права админа)
            string nvidiaSrc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA");
            string nvidiaDest = Path.Combine(localPath, "NVIDIA");
            if (Directory.Exists(nvidiaSrc) && !Directory.Exists(nvidiaDest))
            {
                try
                {
                    // Создаём символическую ссылку через cmd
                    ProcessStartInfo linkInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c mklink /D \"{nvidiaDest}\" \"{nvidiaSrc}\"",
                        UseShellExecute = true,
                        Verb = "runas" // Запуск от админа
                    };
                    Process linkProc = Process.Start(linkInfo);
                    if (linkProc != null)
                    {
                        linkProc.WaitForExit();
                        Console.WriteLine($"[7VB] ✅ Создана ссылка NVIDIA: {nvidiaDest}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[7VB] ⚠️ Не удалось создать ссылку NVIDIA: {ex.Message}");
                }
            }
            
            CS2Optimizer.ConfigureAllFiles((accountInstance.MaFile.Session.SteamID - 76561197960265728).ToString(), SettingsManager.SteamPath, SettingsManager.CS2Path, accountInstance.Login);

            // Уникальное IPC имя для каждого аккаунта
            string ipcName = $"SteamIPC_{accountInstance.MaFile.Session.SteamID}_{Process.GetCurrentProcess().Id}";

            Console.WriteLine($"[7VB] 🚀 Запуск Steam для {accountInstance.Login} (IPC: {ipcName})");
            Console.WriteLine($"[7VB] 📁 Profile: {baseProfilePath}");
            
            // Закрываем мьютексы ПЕРЕД запуском!
            Console.WriteLine($"[7VB] 🔓 Закрытие мьютексов перед запуском...");
            MutexCloser.CloseAllCS2SingletonMutexes();
            Thread.Sleep(2000);

            // Запускаем Steam с изоляцией
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = $"{SettingsManager.SteamPath}" + $"\\steam.exe",
                // -master_ipc_name_override разрешает несколько экземпляров
                // -userdata указывает на изолированную папку userdata
                Arguments = $"-login -nofriendsui -vgui -master_ipc_name_override {ipcName} -noverifyfiles -language english -allowmultiple -multirun -userdata \"{localPath}\\Steam\\userdata\" -applaunch 730 -noborder -con_logfile {accountInstance.Login}.log -exec boost.cfg " + SettingsManager.CS2Arguments,
                UseShellExecute = false
            };

            // Копируем все текущие переменные и переопределяем ключевые
            foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                processStartInfo.EnvironmentVariables[de.Key.ToString()] = de.Value.ToString();
            }

            // Переопределяем ключевые переменные для изоляции
            processStartInfo.EnvironmentVariables["USERPROFILE"] = baseProfilePath;
            processStartInfo.EnvironmentVariables["LOCALAPPDATA"] = localPath;
            processStartInfo.EnvironmentVariables["APPDATA"] = Path.Combine(baseProfilePath, "AppData", "Roaming");

            SteamProcess = new Process();
            SteamProcess.StartInfo = processStartInfo;
            SteamProcess.Exited += delegate
            {
                accountInstance.SetAccountColor(Brushes.White);
                accountInstance.AccountStatus = EAccountStatus.NotStarted;
            };
            SteamProcess.EnableRaisingEvents = true;
            SteamProcess.Start();

            Console.WriteLine($"[7VB] ✅ Steam процесс запущен (PID: {SteamProcess.Id})");

            // Ждём появления окна авторизации (5 секунд)
            Thread.Sleep(5000);

            // Вводим логин и пароль через SteamUtils
            Console.WriteLine($"[7VB] 🔑 Ввод логина и пароля...");
            SteamUtils.WaitForSteamWindowAndLogin(SteamProcess.Id, accountInstance, timeoutSeconds: 120);

            // Ждём 25 секунд пока Steam авторизуется и запустит CS2
            Console.WriteLine($"[7VB] ⏳ Ожидание запуска CS2 (25 секунд)...");
            Thread.Sleep(25000);

            // Ищем CS2, запущенный этим Steam (раньше использовался всегда processes[0] — один PID на всех аккаунтов)
            Process cs2Process = null;
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    var children = ProcessesUtils.GetProcessesByParentPID(SteamProcess.Id, "cs2");
                    if (children == null || children.Length == 0)
                        children = ProcessesUtils.GetDirectChildProcessesByWmi(SteamProcess.Id, "cs2.exe");

                    if (children != null && children.Length > 0)
                    {
                        cs2Process = children
                            .OrderByDescending(p =>
                            {
                                try
                                {
                                    return p.StartTime;
                                }
                                catch
                                {
                                    return DateTime.MinValue;
                                }
                            })
                            .First();
                        Console.WriteLine($"[7VB] ✅ CS2 для {accountInstance.Login}: Steam PID {SteamProcess.Id} → CS2 PID {cs2Process.Id}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[7VB] ⚠️ Поиск CS2: {ex.Message}");
                }

                Thread.Sleep(1000);
            }

            if (cs2Process == null)
            {
                Console.WriteLine($"[7VB] ❌ CS2 не найден!");
                accountInstance.AccountStatus = EAccountStatus.NotStarted;
                accountInstance.SetAccountColor(Brushes.Red);
                return;
            }

            // Закрываем мьютекс для этого CS2
            Console.WriteLine($"[7VB] 🔓 Закрытие мьютекса для CS2 PID {cs2Process.Id}...");
            MutexCloser.CloseCS2SingletonMutex(cs2Process.Id);
            Thread.Sleep(1000);

            // Ждём появления окна
            while (cs2Process.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(500);
            }

            accountInstance.CS2Client.CS2Process = cs2Process;

            accountInstance.CS2Client.CS2_WindowComponent.SyncClientSizeFromArguments();
            accountInstance.CS2Client.CS2_WindowComponent.Setup(cs2Process.MainWindowHandle);

            accountInstance.CS2Client.CS2_WindowComponent.ChangeWindowTitle("[7VB] " + accountInstance.Login);
            accountInstance.AccountStatus = EAccountStatus.InMainMenu;
            accountInstance.SetAccountColor(Brushes.Green);

            try
            {
                accountInstance.CS2CmdComponent.StartReadingConsole();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[7VB] ⚠️ Log watcher: {ex.Message}");
            }

            Console.WriteLine($"[7VB] ✅ CS2 запущен для {accountInstance.Login}");
        }
        public void Stop()
        {
            if (SteamProcess == null) return;
            try
            {
                if (accountInstance.CS2Client != null)
                {
                    accountInstance.CS2Client.Stop();
                }
                SteamProcess.Kill();
                accountInstance.SetAccountColor(Brushes.White);
            }
            catch (Exception e)
            {

            }
        }
    }

}
