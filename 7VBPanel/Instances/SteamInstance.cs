using _7VBPanel.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading;
using _7VBPanel.Utils;
using System.Threading.Tasks;
using System.Security.Principal;
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
            CS2Optimizer.ConfigureAllFiles((accountInstance.MaFile.Session.SteamID - 76561197960265728).ToString(), SettingsManager.SteamPath, SettingsManager.CS2Path, accountInstance.Login);
            SteamProcess = new Process();
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = $"{SettingsManager.SteamPath}" + $"\\steam.exe",
                Arguments = $"-silent -login -nofriendsui -vgui -master_ipc_name_override {accountInstance.Login} -noverifyfiles -nobootstrapupdate -skipinitialbootstrap -norepairfiles -overridepackageurl -disable-winh264 -language english -applaunch 730 -noborder -con_logfile -nosound {accountInstance.Login}.log -exec boost.cfg " + SettingsManager.CS2Arguments,
                UseShellExecute = false
            };
            SteamProcess.StartInfo = processStartInfo;
            SteamProcess.Exited += delegate
            {
                accountInstance.SetAccountColor(Brushes.White);
                accountInstance.AccountStatus = EAccountStatus.NotStarted;
            };
            SteamProcess.EnableRaisingEvents = true;
            SteamProcess.Start();
            
            Thread.Sleep(5000);
            SteamUtils.LoginInSteamWindowFlaUIMethod(SteamProcess.Id, accountInstance);
            accountInstance.AccountStatus = EAccountStatus.WaitCS2;
            new Thread(() =>
            {
                accountInstance.FindCS2();
                accountInstance.CS2Client.CS2_WindowComponent.ChangeWindowTitle("[7VB] " + accountInstance.Login);
                accountInstance.AccountStatus = EAccountStatus.InMainMenu;
                accountInstance.SetAccountColor(Brushes.Green);

            }).Start();
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
