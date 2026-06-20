using _7VBPanel.Instances;
using _7VBPanel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace _7VBPanel.Managers
{
    /// <summary>
    /// Фарм 8:8 без GSI. Один макрос за раунд (ротация окон), первый раунд без ожидания Round_Start.
    /// </summary>
    public static class FarmManager
    {
        private const int FreezeTimeSec = 15;
        private const int AntiAfkAtSec = 6;
        private const int DetectSideAtSec = 14;
        private const int PostMacroWaitSec = 5;
        private const int PresentationSec = 3;
        private const int TargetScore = 8;
        private const int RoundFallbackSec = 28;

        private static volatile bool _running;
        private static Thread _thread;
        private static IReadOnlyList<AccountInstance> _accounts = Array.Empty<AccountInstance>();
        private static readonly HashSet<string> _presentationShown = new HashSet<string>(StringComparer.Ordinal);
        private static int _freezeCycleToken;
        private static int _macroRotationIndex;

        public static bool IsRunning => _running;

        public static event Action OnFarmEnded;

        public static void Start(IReadOnlyList<AccountInstance> accounts)
        {
            if (_running)
                return;

            var active = accounts?
                .Where(a => a?.CS2Client?.CS2Process != null)
                .Where(a =>
                {
                    try { return !a.CS2Client.CS2Process.HasExited; }
                    catch { return false; }
                })
                .ToList();

            if (active == null || active.Count == 0)
            {
                PanelLog.Line("[Farm] ❌ Нет запущенных CS2. Сначала Start аккаунтов.");
                return;
            }

            foreach (var acc in active)
                acc.ResetFarmState();

            _accounts = active;
            _presentationShown.Clear();
            _macroRotationIndex = 0;
            Interlocked.Increment(ref _freezeCycleToken);

            GsiListener.Reset();
            GsiListener.RegisterAccounts(active);
            GsiListener.TeamUpdated -= OnGsiTeamUpdated;
            GsiListener.TeamUpdated += OnGsiTeamUpdated;
            GsiListener.Start();

            _running = true;
            _thread = new Thread(FarmLoop) { IsBackground = true, Name = "7VB-Farm" };
            _thread.Start();
            PanelLog.Line($"[Farm] 🤖 Запуск фарма на {active.Count} аккаунтах (GSI-команда + 1 макрос/раунд)");
        }

        public static void Stop()
        {
            if (!_running)
                return;
            _running = false;
            GsiListener.TeamUpdated -= OnGsiTeamUpdated;
            Interlocked.Increment(ref _freezeCycleToken);
            try { _thread?.Join(3000); } catch { }
            _thread = null;
            PanelLog.Line("[Farm] 🛑 Фарм остановлен.");
        }

        private static void OnGsiTeamUpdated(string login, string team)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(team))
                return;
            foreach (var acc in _accounts)
            {
                if (acc != null && string.Equals(acc.Login, login, StringComparison.OrdinalIgnoreCase))
                {
                    acc.SetFarmTeam(team, "GSI");
                    break;
                }
            }
        }

        private static void FarmLoop()
        {
            try
            {
                PanelLog.Line("[Farm] ⏳ Ожидание InGame на всех окнах...");
                if (!WaitUntil(AllInGame, 180, "InGame"))
                {
                    PanelLog.Line("[Farm] ❌ Не все аккаунты в игре.");
                    return;
                }

                PanelLog.Line($"[Farm] ✅ Все {_accounts.Count} в игре! Счёт {GetGlobalScore()}");
                MaybeShowPresentation();

                // Первый раунд: не ждём Round_Start (часто уже пропущен к моменту нажатия Farm)
                PanelLog.Line("[Farm] 🚀 Первый раунд — фризтайм сразу (без ожидания лога)");
                RunFreezeCycle();
                SleepSec(PostMacroWaitSec);

                int lastRoundTotal = GetGlobalRoundTotal();
                long lastRoundStart = GetMinRoundStarts();

                while (_running)
                {
                    if (IsMatchComplete())
                    {
                        DisconnectAll();
                        PanelLog.Line("[Farm] 🏁 Игра завершена (8:8). Фарм завершён.");
                        break;
                    }

                    PanelLog.Line("[Farm] ⏳ Ожидание нового раунда (счёт в логе / Round_Start)...");
                    int scoreBefore = lastRoundTotal;
                    long startBefore = lastRoundStart;
                    bool nextRound = WaitUntil(
                        () => GetGlobalRoundTotal() > scoreBefore || GetMinRoundStarts() > startBefore,
                        35,
                        "новый раунд");

                    if (!_running) break;

                    if (nextRound)
                    {
                        int scoreNow = GetGlobalRoundTotal();
                        if (scoreNow > scoreBefore)
                            PanelLog.Line($"[Farm] 🔔 Раунд по счёту: {GetGlobalScore()} (всего {scoreNow})");
                        else
                            PanelLog.Line($"[Farm] 🔔 Round_Start #{GetMinRoundStarts()}");
                        lastRoundTotal = Math.Max(scoreBefore, GetGlobalRoundTotal());
                        lastRoundStart = GetMinRoundStarts();
                    }
                    else
                    {
                        PanelLog.Line($"[Farm] ⚠️ Лог молчит — таймер {RoundFallbackSec} сек");
                        SleepSec(RoundFallbackSec);
                        if (!_running) break;
                        lastRoundTotal = GetGlobalRoundTotal();
                        lastRoundStart = GetMinRoundStarts();
                    }

                    MaybeShowPresentation();
                    RunFreezeCycle();
                    SleepSec(PostMacroWaitSec);

                    if (IsMatchComplete())
                    {
                        DisconnectAll();
                        PanelLog.Line("[Farm] 🏁 Игра завершена (8:8). Фарм завершён.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                PanelLog.Line($"[Farm] ❌ Ошибка: {ex.Message}");
            }
            finally
            {
                _running = false;
                OnFarmEnded?.Invoke();
            }
        }

        private static void MaybeShowPresentation()
        {
            string score = GetGlobalScore();
            if ((score == "0:0" || score == "4:4") && _presentationShown.Add(score))
            {
                PanelLog.Line($"[Farm] ⏳ Экран презентации ({PresentationSec} сек) — {score}...");
                SleepSec(PresentationSec);
            }
        }

        private static void RunFreezeCycle()
        {
            int token = _freezeCycleToken;
            PanelLog.Line($"[Farm] ⏳ Фризтайм {FreezeTimeSec} сек... (счёт {GetGlobalScore()})");

            Dictionary<string, string> detectedSides = null;

            for (int elapsed = 0; elapsed < FreezeTimeSec && _running && token == _freezeCycleToken; elapsed++)
            {
                if (elapsed == AntiAfkAtSec)
                    RunAntiAfkAll();

                if (elapsed == DetectSideAtSec)
                {
                    detectedSides = DetectSidesAll();
                    break;
                }

                Thread.Sleep(1000);
            }

            if (!_running || token != _freezeCycleToken)
                return;

            if (detectedSides == null || detectedSides.Count == 0)
            {
                PanelLog.Line("[Farm] ⚠️ Детект на 14с не сработал — повтор...");
                detectedSides = DetectSidesAll();
            }

            RunSingleMacroRotating(detectedSides, token);
        }

        /// <summary>Один макрос за раунд — следующее окно по кругу (как dsa_v16).</summary>
        private static void RunSingleMacroRotating(Dictionary<string, string> sides, int token)
        {
            if (sides == null || sides.Count == 0)
            {
                PanelLog.Line("[Farm] ⚠️ Нет сторон для макроса.");
                return;
            }

            int n = _accounts.Count;
            for (int attempt = 0; attempt < n; attempt++)
            {
                if (!_running || token != _freezeCycleToken)
                    return;

                int idx = (_macroRotationIndex + attempt) % n;
                var acc = _accounts[idx];

                if (!sides.TryGetValue(acc.Login, out string side) || string.IsNullOrEmpty(side))
                    continue;

                if (!TryFocusAccount(acc, out IntPtr hwnd))
                {
                    PanelLog.Line($"[Farm]   ❌ {acc.Login}: окно не найдено, пробуем следующее");
                    continue;
                }

                int slot = (idx % n) + 1;
                PanelLog.Line($"[Farm]   🎯 Макрос {side} → только {acc.Login} (окно {slot}/{n})");
                MacroPlayer.Execute(acc.CS2Client, side, hwnd, () => _running && token == _freezeCycleToken);
                PanelLog.Line($"[Farm]   ✅ {acc.Login}: Макрос {side} ЗАВЕРШЕН!");

                _macroRotationIndex = idx + 1;
                return;
            }

            PanelLog.Line("[Farm] ⚠️ Ни на одном окне не удалось запустить макрос.");
        }

        private static bool AllInGame()
        {
            return _accounts.All(a =>
                a != null && (
                    a.AccountStatus == EAccountStatus.InGame
                    || a.FarmRoundStarts > 0));
        }

        private static long GetMinRoundStarts()
        {
            if (_accounts.Count == 0) return 0;
            return _accounts.Min(a => a.FarmRoundStarts);
        }

        private static string GetGlobalScore()
        {
            int ct = _accounts.Max(a => a.FarmCtScore);
            int t = _accounts.Max(a => a.FarmTScore);
            return $"{ct}:{t}";
        }

        private static int GetGlobalCt() => _accounts.Max(a => a.FarmCtScore);
        private static int GetGlobalT() => _accounts.Max(a => a.FarmTScore);

        private static int GetGlobalRoundTotal() =>
            _accounts.Count == 0 ? 0 : _accounts.Max(a => a.FarmCtScore + a.FarmTScore);

        private static void RunAntiAfkAll()
        {
            PanelLog.Line("[Farm] 🧘 Anti-AFK на всех окнах...");
            foreach (var acc in _accounts)
            {
                if (!_running) return;
                try
                {
                    if (!TryFocusAccount(acc, out _))
                        continue;

                    var sim = acc.CS2Client.inputSimulator ?? new InputSimulator();
                    sim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                    Thread.Sleep(100);
                    sim.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                    PanelLog.Line($"[Farm]   ✅ {acc.Login}: Ctrl");
                    Thread.Sleep(80);
                }
                catch (Exception ex)
                {
                    PanelLog.Line($"[Farm]   ❌ {acc.Login}: Anti-AFK — {ex.Message}");
                }
            }
        }

        private static Dictionary<string, string> DetectSidesAll()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PanelLog.Line("[Farm] 🎮 Детект стороны (GSI → con_log → HUD → Tab → Buy)...");

            foreach (var acc in _accounts)
            {
                if (!_running) break;
                try
                {
                    if (!TryFocusAccount(acc, out IntPtr hwnd))
                    {
                        PanelLog.Line($"[Farm]   ❌ {acc.Login}: окно не найдено");
                        continue;
                    }

                    var sim = acc.CS2Client.inputSimulator ?? new InputSimulator();
                    string side = TeamDetector.DetectForAccount(acc, hwnd, sim);
                    if (!string.IsNullOrEmpty(side))
                        result[acc.Login] = side;
                }
                catch (Exception ex)
                {
                    PanelLog.Line($"[Farm]   ❌ {acc.Login}: детект — {ex.Message}");
                }
            }

            return result;
        }

        private static void DisconnectAll()
        {
            PanelLog.Line("[Farm] 🏁 Счёт 8:8! disconnect на всех окнах...");

            foreach (var acc in _accounts)
            {
                try
                {
                    if (!TryFocusAccount(acc, out _))
                        continue;

                    var sim = acc.CS2Client.inputSimulator ?? new InputSimulator();
                    sim.Keyboard.KeyPress(VirtualKeyCode.OEM_3);
                    Thread.Sleep(400);
                    sim.Keyboard.TextEntry("disconnect");
                    Thread.Sleep(200);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(1200);

                    PanelLog.Line($"[Farm]   ✅ {acc.Login}: disconnect");
                }
                catch (Exception ex)
                {
                    PanelLog.Line($"[Farm]   ❌ {acc.Login}: disconnect — {ex.Message}");
                }
            }

            PanelLog.Line("[Farm] 🏁 Все аккаунты отключены!");
        }

        private static bool TryFocusAccount(AccountInstance acc, out IntPtr hwnd)
        {
            hwnd = IntPtr.Zero;
            if (acc?.CS2Client?.CS2Process == null)
                return false;

            try
            {
                if (acc.CS2Client.CS2Process.HasExited)
                    return false;
                acc.CS2Client.CS2Process.Refresh();
            }
            catch
            {
                return false;
            }

            hwnd = Win32.FindLargestTopLevelWindowForProcessId(acc.CS2Client.CS2Process.Id);
            if (hwnd == IntPtr.Zero)
                hwnd = acc.CS2Client.CS2Process.MainWindowHandle;
            if (hwnd == IntPtr.Zero)
                return false;

            acc.CS2Client.CS2_WindowComponent?.RebindWindowHandle(hwnd);
            Win32.BringWindowToFront(hwnd);
            Thread.Sleep(250);
            return true;
        }

        private static bool IsScore8x8()
        {
            return GetGlobalCt() >= TargetScore && GetGlobalT() >= TargetScore;
        }

        private static bool IsMatchComplete()
        {
            if (IsScore8x8())
                return true;
            if (GetGlobalRoundTotal() >= 16)
            {
                PanelLog.Line("[Farm] 📊 16 раундов — завершение");
                return true;
            }
            return false;
        }

        private static bool WaitUntil(Func<bool> condition, int timeoutSec, string label)
        {
            for (int i = 0; i < timeoutSec * 2 && _running; i++)
            {
                if (condition())
                    return true;
                Thread.Sleep(500);
            }
            if (_running)
                PanelLog.Line($"[Farm] ⚠️ Таймаут {label} ({timeoutSec}с)");
            return false;
        }

        private static void SleepSec(int seconds)
        {
            for (int i = 0; i < seconds && _running; i++)
                Thread.Sleep(1000);
        }
    }
}
