using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SteamAuth;
using System.Threading.Tasks;
using _7VBPanel.Utils;

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
        public Brush Color = Brushes.White;
        public delegate void ColorChangeHandler(string Login, Brush NewColor);

        public event ColorChangeHandler OnColorChangedEvent;

        public AccountInstance(string login, string password)
        {
            Login = login;
            Password = password;
          
            AccountStatus = EAccountStatus.NotStarted;
            SteamClient = new SteamInstance(this);
            CS2Client = new CS2Instance(this);
        }
        public void FindCS2()
        {
            CS2Client = new CS2Instance(this);
            Process CS2Process = null;
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + $"{SteamClient.SteamProcess.Id:D4}");
            while (CS2Process == null)
            {
                foreach (ManagementObject item in managementObjectSearcher.Get())
                {
                    if (item["NAME"].ToString() == "cs2.exe")
                    {
                        CS2Process = Process.GetProcessById(Convert.ToInt32((uint)item["PROCESSID"]));
                        break;
                    }
                }
                Thread.Sleep(500);
            }
            Thread.Sleep(10000);
            CS2Client.CS2Process = CS2Process;
            while(CS2Process.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(1000);
            }
            CS2Client.CS2_WindowComponent.Setup(CS2Process.MainWindowHandle);
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
