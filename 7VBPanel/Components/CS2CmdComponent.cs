using _7VBPanel.Instances;
using _7VBPanel.Managers;
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
