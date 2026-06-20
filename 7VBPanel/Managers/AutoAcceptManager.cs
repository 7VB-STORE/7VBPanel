using _7VBPanel.Instances;
using _7VBPanel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace _7VBPanel.Managers
{
    /// <summary>
    /// Как FSN <see href="https://github.com/idk-this/FSN_Panel/blob/main/Modules/AutoAcceptModule.py">AutoAcceptModule</see>:
    /// при совпадении <c>last_match_id</c> (base62 из con_log) у всех в группе — клик в центр окна.
    /// Match id парсится из <c>match_id=(число)</c> в <c>Login</c>.log, как в
    /// <see href="https://raw.githubusercontent.com/idk-this/FSN_Panel/main/Instances/AccountInstance.py">AccountInstance</see>.
    /// </summary>
    public static class AutoAcceptManager
    {
        private static volatile bool _running;
        private static Thread _thread;

        public static bool IsRunning => _running;

        public static void Start()
        {
            if (_running)
                return;
            _running = true;
            _thread = new Thread(Loop) { IsBackground = true, Name = "7VB-AutoAccept" };
            _thread.Start();
        }

        public static void Stop()
        {
            _running = false;
            try
            {
                _thread?.Join(1500);
            }
            catch
            {
            }
            _thread = null;
        }

        public static void Toggle()
        {
            if (_running)
                Stop();
            else
                Start();
        }

        private static void Loop()
        {
            while (_running)
            {
                try
                {
                    CheckOnce();
                }
                catch
                {
                }
                Thread.Sleep(1000);
            }
        }

        private static void CheckOnce()
        {
            IReadOnlyList<AccountInstance> group = GetGroupAccounts().ToList();
            if (group.Count < 2)
                return;
            if (!group.Any(a => !string.IsNullOrEmpty(a?.LastMatchIdCompact)))
                return;

            if (IsGroupAlreadyAcceptedThisMatch(group))
            {
                return;
            }

            Thread.Sleep(1000);
            if (IsGroupAlreadyAcceptedThisMatch(group))
                return;
            if (AllSameMatchId(group) && !string.IsNullOrEmpty(group[0]?.LastMatchIdCompact))
            {
                AcceptGroup(group);
                return;
            }

            Thread.Sleep(1000);
            if (IsGroupAlreadyAcceptedThisMatch(group))
                return;
            if (AllSameMatchId(group) && !string.IsNullOrEmpty(group[0]?.LastMatchIdCompact))
            {
                AcceptGroup(group);
                return;
            }

            foreach (var a in group)
                a?.ClearLastMatchId();
        }

        /// <summary>
        /// Уже обработали этот поиск/матч: не вызывать Accept снова.
        /// Важно: после <see cref="AccountInstance.MarkMatchAcceptedAndClearPending"/> у всех <c>LastMatchIdCompact == null</c> —
        /// старая проверка через <see cref="AllSameMatchId"/> никогда не видела «уже принято» и снова шла в клики.
        /// </summary>
        private static bool IsGroupAlreadyAcceptedThisMatch(IReadOnlyList<AccountInstance> group)
        {
            if (group == null || group.Count == 0)
                return false;
            string a0 = group[0].LastAcceptedMatchIdCompact;
            if (string.IsNullOrEmpty(a0) || !group.All(a => a != null && a.LastAcceptedMatchIdCompact == a0))
                return false;
            string pending0 = group[0].LastMatchIdCompact;
            if (string.IsNullOrEmpty(pending0))
            {
                return true;
            }
            if (!group.All(a => a != null && a.LastMatchIdCompact == pending0))
                return false;
            return pending0 == a0;
        }

        private static bool AllSameMatchId(IReadOnlyList<AccountInstance> group)
        {
            if (group == null || group.Count == 0)
                return false;
            string first = group[0]?.LastMatchIdCompact;
            if (string.IsNullOrEmpty(first))
                return false;
            return group.All(a => a != null && a.LastMatchIdCompact == first);
        }

        private static void AcceptGroup(IReadOnlyList<AccountInstance> group)
        {
            string id = group[0]?.LastMatchIdCompact;
            if (string.IsNullOrEmpty(id))
                return;
            foreach (var acc in group)
            {
                acc?.MarkMatchAcceptedAndClearPending(id);
            }
            PanelLog.Line("[AutoAccept] Старт кликов: base62=" + id + " (в логе выше смотри строки [match_id] с тем же id). Повтор не пойдёт, пока в con_log не будет другого match_id.");

            foreach (var acc in group)
            {
                if (acc?.CS2Client == null)
                    continue;
                try
                {
                    if (acc.CS2Client.CS2Process == null)
                        continue;
                    acc.CS2Client.CS2Process.Refresh();
                    if (acc.CS2Client.CS2Process.HasExited)
                        continue;
                }
                catch
                {
                    continue;
                }
                acc.CS2Client.TryClickMatchAcceptButton(autoAcceptGroupWave: true);
                Thread.Sleep(200);
            }
        }

        private static IEnumerable<AccountInstance> GetGroupAccounts()
        {
            if (LobbyState.Team1 != null && LobbyState.Team2 != null)
            {
                foreach (var a in Flatten(LobbyState.Team1)) yield return a;
                foreach (var a in Flatten(LobbyState.Team2)) yield return a;
                yield break;
            }
            if (LobbyState.Team1 != null)
            {
                foreach (var a in Flatten(LobbyState.Team1)) yield return a;
                yield break;
            }

            foreach (var a in AccountManager.AccountList)
            {
                if (a?.CS2Client?.CS2Process == null) continue;
                try
                {
                    if (a.CS2Client.CS2Process.HasExited) continue;
                }
                catch { continue; }
                yield return a;
            }
        }

        private static IEnumerable<AccountInstance> Flatten(LobbyInstance lobby)
        {
            if (lobby == null) yield break;
            if (lobby.Leader != null) yield return lobby.Leader;
            if (lobby.Bots == null) yield break;
            foreach (var b in lobby.Bots)
            {
                if (b != null) yield return b;
            }
        }
    }
}
