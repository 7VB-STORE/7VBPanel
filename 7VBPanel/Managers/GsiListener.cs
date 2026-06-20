using _7VBPanel.Instances;
using _7VBPanel.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace _7VBPanel.Managers
{
    public class AccountGsiState
    {
        public string Phase = "MENU";
        public string Score = "0:0";
        public int CtScore;
        public int TScore;
        public bool IsLive;
        public string Team;
        public DateTime UpdatedUtc = DateTime.MinValue;
        public DateTime TeamUpdatedUtc = DateTime.MinValue;
    }

    /// <summary>
    /// HTTP-сервер GSI на порту 3000 (как gamestate_integration_GSI.cfg в CS2Optimizer).
    /// </summary>
    public static class GsiListener
    {
        private const int Port = 3000;
        private static Thread _thread;
        private static volatile bool _running;
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, AccountGsiState> ByLogin = new Dictionary<string, AccountGsiState>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> SteamIdToLogin = new Dictionary<string, string>();

        public static string GlobalScore { get; private set; } = "0:0";
        public static int GlobalCtScore { get; private set; }
        public static int GlobalTScore { get; private set; }

        public static event Action<string, int, int> ScoreChanged;
        public static event Action<string, string> TeamUpdated;

        public static void Start()
        {
            if (_running)
                return;
            _running = true;
            _thread = new Thread(ListenLoop) { IsBackground = true, Name = "7VB-GSI" };
            _thread.Start();
        }

        public static void Stop()
        {
            _running = false;
            try { _thread?.Join(2000); } catch { }
            _thread = null;
        }

        public static void Reset()
        {
            lock (Lock)
            {
                ByLogin.Clear();
                SteamIdToLogin.Clear();
                GlobalScore = "0:0";
                GlobalCtScore = 0;
                GlobalTScore = 0;
            }
        }

        public static void RegisterAccounts(IEnumerable<AccountInstance> accounts)
        {
            lock (Lock)
            {
                SteamIdToLogin.Clear();
                foreach (var acc in accounts)
                {
                    if (acc?.MaFile?.Session == null)
                        continue;
                    string sid = acc.MaFile.Session.SteamID.ToString();
                    SteamIdToLogin[sid] = acc.Login;
                    if (!ByLogin.ContainsKey(acc.Login))
                        ByLogin[acc.Login] = new AccountGsiState();
                }
            }
        }

        public static AccountGsiState GetState(string login)
        {
            lock (Lock)
            {
                if (ByLogin.TryGetValue(login, out var s))
                    return s;
                return new AccountGsiState();
            }
        }

        /// <summary>CT/T из GSI player.team (самый точный источник).</summary>
        public static string TryGetTeam(string login)
        {
            if (string.IsNullOrEmpty(login))
                return null;
            lock (Lock)
            {
                if (!ByLogin.TryGetValue(login, out var s))
                    return null;
                if (string.IsNullOrEmpty(s.Team))
                    return null;
                if (DateTime.UtcNow - s.TeamUpdatedUtc > TimeSpan.FromMinutes(3))
                    return null;
                return s.Team;
            }
        }

        public static bool AllAccountsLive(IReadOnlyList<AccountInstance> accounts)
        {
            if (accounts == null || accounts.Count == 0)
                return false;
            lock (Lock)
            {
                foreach (var acc in accounts)
                {
                    if (acc == null) return false;
                    if (!ByLogin.TryGetValue(acc.Login, out var s) || !s.IsLive)
                        return false;
                }
                return true;
            }
        }

        public static bool AllAccountsFreezeTime(IReadOnlyList<AccountInstance> accounts)
        {
            if (accounts == null || accounts.Count == 0)
                return false;
            lock (Lock)
            {
                foreach (var acc in accounts)
                {
                    if (acc == null) return false;
                    if (!ByLogin.TryGetValue(acc.Login, out var s))
                        return false;
                    if (!string.Equals(s.Phase, "FREEZETIME", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            }
        }

        private static void ListenLoop()
        {
            HttpListener listener = null;
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                listener.Prefixes.Add($"http://localhost:{Port}/");
                listener.Start();
                PanelLog.Line($"[Farm] GSI сервер запущен на порту {Port}");
            }
            catch (Exception ex)
            {
                PanelLog.Line($"[Farm] ❌ GSI сервер не запустился: {ex.Message}");
                _running = false;
                return;
            }

            while (_running)
            {
                try
                {
                    var ctx = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
                }
                catch (HttpListenerException)
                {
                    if (!_running) break;
                }
                catch (Exception ex)
                {
                    if (_running)
                        PanelLog.Line($"[Farm] GSI ошибка: {ex.Message}");
                }
            }

            try { listener?.Stop(); } catch { }
        }

        private static void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                string body;
                using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? Encoding.UTF8))
                    body = reader.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(body))
                    ParseGsi(body);

                var buf = Encoding.UTF8.GetBytes("OK");
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentLength64 = buf.Length;
                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            }
            catch
            {
                try { ctx.Response.StatusCode = 500; } catch { }
            }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
            }
        }

        private static void ParseGsi(string json)
        {
            JObject root;
            try { root = JObject.Parse(json); }
            catch { return; }

            string steamId = root["player"]?["steamid"]?.ToString();
            if (string.IsNullOrEmpty(steamId))
                return;

            string login;
            lock (Lock)
            {
                if (!SteamIdToLogin.TryGetValue(steamId, out login))
                    return;
            }

            string teamNorm = TeamHelper.Normalize(root["player"]?["team"]?.ToString());
            bool teamChanged = false;

            var map = root["map"];
            string phase = "MENU";
            int ct = 0;
            int t = 0;
            string score = "0:0";
            bool isLive = false;
            bool scoreChanged = false;
            bool becameLive = false;

            if (map != null)
            {
                phase = (map["phase"]?.ToString() ?? "MENU").ToUpperInvariant();
                ct = map["team_ct"]?["score"]?.Value<int>() ?? 0;
                t = map["team_t"]?["score"]?.Value<int>() ?? 0;
                score = $"{ct}:{t}";
                isLive = phase == "LIVE" || phase == "FREEZETIME";
            }

            lock (Lock)
            {
                if (!ByLogin.TryGetValue(login, out var state))
                {
                    state = new AccountGsiState();
                    ByLogin[login] = state;
                }

                if (!string.IsNullOrEmpty(teamNorm) && state.Team != teamNorm)
                {
                    state.Team = teamNorm;
                    state.TeamUpdatedUtc = DateTime.UtcNow;
                    teamChanged = true;
                }
                else if (!string.IsNullOrEmpty(teamNorm))
                {
                    state.TeamUpdatedUtc = DateTime.UtcNow;
                }

                if (map != null)
                {
                    becameLive = isLive && !state.IsLive;
                    state.Phase = phase;
                    state.CtScore = ct;
                    state.TScore = t;
                    state.Score = score;
                    state.IsLive = isLive;
                    state.UpdatedUtc = DateTime.UtcNow;

                    if (score != GlobalScore)
                    {
                        GlobalScore = score;
                        GlobalCtScore = ct;
                        GlobalTScore = t;
                        scoreChanged = true;
                    }
                }
            }

            if (teamChanged)
                TeamUpdated?.Invoke(login, teamNorm);

            if (becameLive)
                PanelLog.Line($"[Farm] 🟢 {login}: LIVE ({phase})");

            if (scoreChanged)
            {
                int winsTo8 = Math.Max(0, 8 - Math.Max(ct, t));
                PanelLog.Line($"[Farm] 📊 Счёт: {score} (Раундов: {ct + t})");
                PanelLog.Line($"[Farm] 🎯 До 8:8 ещё {winsTo8} побед");
                ScoreChanged?.Invoke(score, ct, t);
            }
        }
    }
}
