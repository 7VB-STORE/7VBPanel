using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace _7VBPanel.Instances
{
    /// <summary>
    /// Утилита для закрытия мьютексов CS2 через handle.exe
    /// </summary>
    public static class MutexCloser
    {
        private static string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mutex_log.txt");
        
        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        private static string GetHandleExePath()
        {
            string handlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "handle.exe");
            if (!File.Exists(handlePath))
            {
                handlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Files", "handle.exe");
            }
            return handlePath;
        }

        private static string RunHandleCommand(string args)
        {
            string handlePath = GetHandleExePath();
            if (!File.Exists(handlePath))
            {
                Log($"❌ handle.exe не найден по пути: {handlePath}");
                return string.Empty;
            }

            try
            {
                // Копируем handle.exe во временную папку (он должен запускаться от туда)
                string tempHandle = Path.Combine(Path.GetTempPath(), "handle.exe");
                if (!File.Exists(tempHandle) || File.GetLastWriteTime(handlePath) != File.GetLastWriteTime(tempHandle))
                {
                    File.Copy(handlePath, tempHandle, true);
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = tempHandle,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetTempPath() // Запускаем из временной папки!
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    Log($"handle.exe output ({args}): {output}");
                    return output;
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка запуска handle.exe: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Закрыть мьютекс csgo_singleton_mutex для конкретного CS2
        /// </summary>
        public static bool CloseCS2SingletonMutex(int cs2Pid)
        {
            if (cs2Pid <= 0)
            {
                Log($"⚠️ Неверный PID: {cs2Pid}");
                return false;
            }

            Log($"🔍 Поиск мьютекса для CS2 PID {cs2Pid}...");

            try
            {
                string[] searchVariants = new string[]
                {
                    $"-accepteula -nobanner -a -p {cs2Pid} csgo_singleton_mutex",
                    $"-accepteula -a -p {cs2Pid} csgo_singleton_mutex",
                    $"-accepteula -p {cs2Pid} -a csgo_singleton_mutex"
                };

                List<string> handles = new List<string>();
                foreach (string args in searchVariants)
                {
                    Log($"Поиск: {args}");
                    string output = RunHandleCommand(args);
                    handles = ParseHandleOutput(output, "csgo_singleton_mutex", "Mutant");
                    if (handles.Count > 0)
                    {
                        Log($"✅ Найдено хэндлов: {handles.Count}");
                        break;
                    }
                }

                if (handles.Count == 0)
                {
                    Log($"⚠️ Мьютексы не найдены для PID {cs2Pid}");
                    return false;
                }

                bool closedAny = false;
                foreach (string handleId in handles)
                {
                    Log($"Закрытие хэндла {handleId}...");
                    string result = RunHandleCommand($"-accepteula -nobanner -c {handleId} -p {cs2Pid} -y");
                    string lowerResult = result.ToLower();
                    if (string.IsNullOrWhiteSpace(result) || 
                        lowerResult.Contains("closed") || 
                        lowerResult.Contains("handle closed") ||
                        lowerResult.Contains("заверш"))
                    {
                        closedAny = true;
                        Log($"✅ Закрыт хэндл {handleId} для CS2 PID {cs2Pid}");
                    }
                    else
                    {
                        Log($"⚠️ Не удалось закрыть {handleId}: {result}");
                    }
                }

                return closedAny;
            }
            catch (Exception ex)
            {
                Log($"Ошибка закрытия мьютекса CS2: {ex.Message}");
                return false;
            }
        }

        private static List<string> ParseHandleOutput(string output, string nameFilter, string typeFilter)
        {
            List<string> handles = new List<string>();
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Log($"Анализ вывода handle.exe...");
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                if (trimmedLine.ToLower().Contains(nameFilter.ToLower()) && 
                    trimmedLine.ToLower().Contains(typeFilter.ToLower()))
                {
                    Log($"Найдено: {trimmedLine}");
                    
                    // Исправленное регулярное выражение - ищем шестнадцатеричное число перед ":"
                    // Формат: "cs2.exe pid: 15256 type: Mutant 2F8: \Sessions\..."
                    Match match = Regex.Match(trimmedLine, @"\b([0-9A-Fa-f]{3,}):", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string handleId = match.Groups[1].Value.ToUpper();
                        // Проверяем, что это не "pid" (оно тоже заканчивается на :)
                        if (handleId != "PID" && !trimmedLine.Substring(0, match.Index).EndsWith("pid"))
                        {
                            handles.Add(handleId);
                            Log($"Добавлен хэндл: {handleId}");
                        }
                    }
                }
            }

            return handles;
        }

        /// <summary>
        /// Закрыть все мьютексы CS2 для всех запущенных процессов
        /// </summary>
        public static void CloseAllCS2SingletonMutexes(int primaryPid = -1)
        {
            Log($"🔍 Поиск процессов CS2...");
            
            List<int> pids = new List<int>();
            if (primaryPid > 0) pids.Add(primaryPid);

            try
            {
                foreach (Process proc in Process.GetProcessesByName("cs2"))
                {
                    if (proc.Id > 0 && !pids.Contains(proc.Id))
                    {
                        pids.Add(proc.Id);
                        Log($"Найден CS2 PID: {proc.Id}");
                    }
                }
            }
            catch { }

            if (pids.Count == 0)
            {
                Log($"Процессы CS2 не найдены");
                return;
            }

            foreach (int cs2Pid in pids)
            {
                try
                {
                    CloseCS2SingletonMutex(cs2Pid);
                }
                catch (Exception ex)
                {
                    Log($"Ошибка: {ex.Message}");
                }
            }
        }
    }
}
