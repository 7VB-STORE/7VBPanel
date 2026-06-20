using _7VBPanel.Instances;
using _7VBPanel.Managers;
using _7VBPanel.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using System.Threading.Tasks;

namespace _7VBPanel.Components
{
    public class CS2CmdComponent
    {
        private AccountInstance accountInstance;
        public Thread ConsoleListenThread;
        public void Setup(AccountInstance Owner)
        {
            accountInstance = Owner;
        }

        public void StartReadingConsole()
        {
            try
            {
                while (!File.Exists(SettingsManager.CS2Path + "\\game\\csgo\\" + accountInstance.Login + ".log"))
                {
                    Thread.Sleep(50);
                }

                Thread.Sleep(6000);
                ConsoleListenThread = new Thread(ReadingThread);
                ConsoleListenThread.SetApartmentState(ApartmentState.STA);
                ConsoleListenThread.Start();
            }
            catch (Exception)
            {
            }
        }
        public void ClearCMDFile()
        {
            FileStream stream = new FileStream(SettingsManager.CS2Path + "\\game\\csgo\\" + accountInstance.Login + ".log", FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter streamWriter = new StreamWriter(stream);
            streamWriter.Write(string.Empty);
        }
        public void ReadingThread()
        {
            while (true)
            {
                Thread.Sleep(500);
                bool NeedCleanLogFile = false;
                string text;
                using (FileStream stream = new FileStream(SettingsManager.CS2Path + "\\game\\csgo\\" + accountInstance.Login + ".log", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    StreamReader streamReader = new StreamReader(stream);
                    text = streamReader.ReadToEnd();
                }
                // Как FSN process_log_line: match_id=(\d+) → base62, общий id для AutoAccept
                var matchLog = Regex.Matches(text, @"match_id=(\d+)", RegexOptions.IgnoreCase);
                if (matchLog.Count > 0
                    && long.TryParse(matchLog[matchLog.Count - 1].Groups[1].Value, out long matchId))
                {
                    accountInstance.SetLastMatchIdFromLogDecimal(matchId);
                }

                FarmLogParser.Count(text, out int ctScored, out int tScored, out long roundStarts);
                accountInstance.ApplyFarmLogCounts(ctScored, tScored, roundStarts);

                string teamFromLog = FarmLogParser.TryParsePlayerTeam(text);
                if (!string.IsNullOrEmpty(teamFromLog))
                    accountInstance.SetFarmTeam(teamFromLog, "con_log");

                if (text.Contains("CSGO_GAME_UI_STATE_INGAME -> CSGO_GAME_UI_STATE_MAINMENU"))
                {
                    accountInstance.AccountStatus = EAccountStatus.InMainMenu;
                    NeedCleanLogFile = true;
                }
                if (text.Contains("CSGO_GAME_UI_STATE_LOADINGSCREEN -> CSGO_GAME_UI_STATE_INGAME"))
                {
                    accountInstance.AccountStatus = EAccountStatus.InGame;
                    NeedCleanLogFile = true;
                }
                
                if (text.Contains("CSGO_GAME_UI_STATE_MAINMENU -> CSGO_GAME_UI_STATE_LOADINGSCREEN"))
                {
                    accountInstance.AccountStatus = EAccountStatus.InLoading;
                    NeedCleanLogFile = true;
                }
                if (text.Contains("CSGO_GAME_UI_STATE_LOADINGSCREEN -> CSGO_GAME_UI_STATE_MAINMENU"))
                {
                    accountInstance.AccountStatus = EAccountStatus.InMainMenu;
                    NeedCleanLogFile = true;
                }
                if (NeedCleanLogFile)
                {
                    ClearCMDFile();
                }
            }
        }
    }

}
