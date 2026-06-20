using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading;
using SteamAuth;
using System.Threading.Tasks;
using _7VBPanel.Utils;
using _7VBPanel.Components;

namespace _7VBPanel.Instances
{
    public enum EAccountStatus
    {
        NotStarted,
        Starting,
        WaitCS2,
        InMainMenu,
        InLoading,
        InGame
    };
    public class AccountInstance
    {
        public string Login;
        public string Password;
        public SteamGuardAccount MaFile;
        public EAccountStatus AccountStatus;
        public SteamInstance SteamClient;
        public CS2Instance CS2Client;
        public CS2CmdComponent CS2CmdComponent;
        public Brush Color = Brushes.White;

        private readonly object _matchIdLock = new object();
        private string _lastMatchIdCompact;
        /// <summary>После успешного AutoAccept: этот match id больше не кликать, пока в логе не появится другой id.</summary>
        private string _lastAcceptedMatchIdCompact;

        /// <summary>Base62 — как FSN после парсинга <c>match_id=(\d+)</c> из con_log.</summary>
        public string LastMatchIdCompact
        {
            get
            {
                lock (_matchIdLock)
                    return _lastMatchIdCompact;
            }
        }

        public string LastAcceptedMatchIdCompact
        {
            get
            {
                lock (_matchIdLock)
                    return _lastAcceptedMatchIdCompact;
            }
        }

        public void SetLastMatchIdFromLogDecimal(long matchIdDecimal)
        {
            string compact = Base62Utils.ToBase62(matchIdDecimal);
            lock (_matchIdLock)
            {
                bool valueChanged = _lastMatchIdCompact != compact;
                if (_lastMatchIdCompact != null && _lastMatchIdCompact != compact)
                    _lastAcceptedMatchIdCompact = null;
                _lastMatchIdCompact = compact;
                if (valueChanged)
                    PanelLog.Line($"[match_id] {Login} из лога: {matchIdDecimal} → {compact} (base62, для AutoAccept)");
            }
        }

        public void ClearLastMatchId()
        {
            lock (_matchIdLock)
                _lastMatchIdCompact = null;
        }

        /// <summary>Запомнить принятый матч и сбросить pending, чтобы тот же id из лога не вызывал повторных кликов.</summary>
        public void MarkMatchAcceptedAndClearPending(string matchIdCompact)
        {
            if (string.IsNullOrEmpty(matchIdCompact))
                return;
            lock (_matchIdLock)
            {
                _lastAcceptedMatchIdCompact = matchIdCompact;
                _lastMatchIdCompact = null;
            }
        }
        // --- Farm (con_log, без GSI) ---
        private readonly object _farmLock = new object();
        private int _farmCtScore;
        private int _farmTScore;
        private long _farmRoundStarts;
        private string _farmTeam;
        private DateTime _farmTeamUtc = DateTime.MinValue;
        private string _farmTeamSource;

        public string FarmTeamSource
        {
            get { lock (_farmLock) return _farmTeamSource; }
        }

        public int FarmCtScore { get { lock (_farmLock) return _farmCtScore; } }
        public int FarmTScore { get { lock (_farmLock) return _farmTScore; } }
        public long FarmRoundStarts { get { lock (_farmLock) return _farmRoundStarts; } }
        public string FarmScore
        {
            get
            {
                lock (_farmLock)
                    return $"{_farmCtScore}:{_farmTScore}";
            }
        }

        public void ResetFarmState()
        {
            lock (_farmLock)
            {
                _farmCtScore = 0;
                _farmTScore = 0;
                _farmRoundStarts = 0;
                _farmTeam = null;
                _farmTeamSource = null;
                _farmTeamUtc = DateTime.MinValue;
            }
        }

        public string GetFarmTeamIfFresh(TimeSpan maxAge)
        {
            lock (_farmLock)
            {
                if (string.IsNullOrEmpty(_farmTeam))
                    return null;
                if (DateTime.UtcNow - _farmTeamUtc > maxAge)
                    return null;
                return _farmTeam;
            }
        }

        public void SetFarmTeam(string side, string source)
        {
            side = Utils.TeamHelper.Normalize(side);
            if (string.IsNullOrEmpty(side))
                return;

            lock (_farmLock)
            {
                bool changed = _farmTeam != side;
                _farmTeam = side;
                _farmTeamSource = source;
                _farmTeamUtc = DateTime.UtcNow;
                if (changed)
                    PanelLog.Line($"[Farm] 🎽 {Login}: команда {side} ({source})");
            }
        }

        public void ApplyFarmLogCounts(int ctScored, int tScored, long roundStarts)
        {
            int newCt = 0, newT = 0;
            long newRounds = 0;
            lock (_farmLock)
            {
                if (ctScored > _farmCtScore) { newCt = ctScored - _farmCtScore; _farmCtScore = ctScored; }
                if (tScored > _farmTScore) { newT = tScored - _farmTScore; _farmTScore = tScored; }
                if (roundStarts > _farmRoundStarts) { newRounds = roundStarts - _farmRoundStarts; _farmRoundStarts = roundStarts; }
            }
            if (newCt > 0 || newT > 0)
            {
                int winsTo8 = Math.Max(0, 8 - Math.Max(FarmCtScore, FarmTScore));
                PanelLog.Line($"[Farm] 📊 {Login} счёт {FarmScore} (Раундов: {FarmCtScore + FarmTScore})");
                PanelLog.Line($"[Farm] 🎯 До 8:8 ещё {winsTo8} побед");
            }
            if (newRounds > 0)
                PanelLog.Line($"[Farm] 🔔 {Login}: Round_Start ×{newRounds} (фризтайм)");
        }

        public delegate void ColorChangeHandler(string Login, Brush NewColor);

        public event ColorChangeHandler OnColorChangedEvent;

        public AccountInstance(string login, string password)
        {
            Login = login;
            Password = password;

            AccountStatus = EAccountStatus.NotStarted;
            SteamClient = new SteamInstance(this);
            CS2Client = new CS2Instance(this);
            CS2CmdComponent = new CS2CmdComponent();
            CS2CmdComponent.Setup(this);
        }
        public void SetAccountColor(Brush NewColor)
        {
            Color = NewColor;
            if (OnColorChangedEvent != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnColorChangedEvent(Login, NewColor);
                }));
            }
        }

     
    }

}
