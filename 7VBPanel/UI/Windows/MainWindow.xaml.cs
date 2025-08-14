using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using _7VBPanel.Managers;
using System.Collections.ObjectModel;
using _7VBPanel.Utils;
using _7VBPanel.Structures;
using System.Text.RegularExpressions;
using _7VBPanel.UI.Elements;
using System.Threading;
using _7VBPanel.Instances;
using System.Security.Principal;

using System.Runtime.InteropServices;
using FlaUI.Core.Conditions;
using SharpDX.DXGI;
using SharpDX;
using FlaUI.Core.WindowsAPI;
using HidSharp;

namespace _7VBPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> VideoAdapters { get; set; }

        private void AddItem(string name, Brush Color)
        {
            ToggleSelection checkBox = new ToggleSelection();
            checkBox.ToggleText = name;
            checkBox.TextColor = Color;
            AccountListBox.Items.Add(checkBox);
        }
        LobbyInstance Team1;
        LobbyInstance Team2;
        public MainWindow()
        {
            InitializeComponent();

            SettingsManager.LoadSettings();
            AccountManager.LoadAccounts();


        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HardwareUtils.InitializeHardwareMonitor();
            using (var factory = new Factory1())
            {
                Adapter highPerformanceAdapter = null;
                long maxVRAM = 0;

                foreach (var adapter in factory.Adapters)
                {
                    var description = adapter.Description;

                    long adapterVRAM = description.DedicatedVideoMemory;

                    if (adapterVRAM > maxVRAM)
                    {
                        maxVRAM = adapterVRAM;
                        highPerformanceAdapter = adapter;
                    }

                }

                if (highPerformanceAdapter != null)
                {
                    var desc = highPerformanceAdapter.Description;

                    int vendorId = desc.VendorId;
                    int deviceId = desc.DeviceId;
                }
            }
            CS2ArgumentsTextBox.Text = SettingsManager.CS2Arguments;
            bool IsSteamFolder = FileExistsIgnoreCase(SettingsManager.SteamPath, "Steam.exe");
            SteamPathBtn.ButtonCircleColor = IsSteamFolder ? Brushes.Green : Brushes.Red;
            string cs2Path = System.IO.Path.Combine(SettingsManager.CS2Path, "game", "bin", "win64");
            bool IsCS2Folder = FileExistsIgnoreCase(cs2Path, "cs2.exe");
            CS2PathBtn.ButtonCircleColor = IsCS2Folder ? Brushes.Green : Brushes.Red;



            foreach (var account in AccountManager.AccountList)
            {
                AddItem(account.Login, account.Color);
                account.OnColorChangedEvent += OnColorChanged;
            }
        }
        private void OnColorChanged(string Login, Brush NewColor)
        {
            foreach (ToggleSelection account in AccountListBox.Items)
            {
                account.Dispatcher.Invoke(() =>
                {
                    if (Login == account.ToggleText)
                    {
                        account.TextColor = NewColor;
                    }
                });
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left ||
                e.GetPosition(this).Y >= 30) return;
            try
            {
                DragMove();
            }
            catch (Exception ex)
            { }
        }

        private void CloseButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void SelectPathButton_ButtonClick(object sender, RoutedEventArgs e)
        {

            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                bool IsSteamFolder = FileExistsIgnoreCase(dialog.SelectedPath, "Steam.exe");
                SteamPathBtn.ButtonCircleColor = IsSteamFolder ? Brushes.Green : Brushes.Red;
                if (IsSteamFolder)
                {
                    SettingsManager.SteamPath = dialog.SelectedPath;
                    SettingsManager.SaveSettings();
                }
            }
        }
        static bool FileExistsIgnoreCase(string directoryPath, string fileName)
        {
            if (directoryPath == null) return false;
            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            if (!directory.Exists)
            {
                return false;
            }

            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                if (string.Equals(file.Name, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        private void SelectPathButton_ButtonClick_1(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                string cs2Path = System.IO.Path.Combine(dialog.SelectedPath, "game", "bin", "win64");
                bool IsCS2Folder = FileExistsIgnoreCase(cs2Path, "cs2.exe");
                CS2PathBtn.ButtonCircleColor = IsCS2Folder ? Brushes.Green : Brushes.Red;
                if (IsCS2Folder)
                {
                    SettingsManager.CS2Path = dialog.SelectedPath;
                    SettingsManager.SaveSettings();
                }
            }
        }

        private void ButtonWIthTextOnly_ButtonClick(object sender, RoutedEventArgs e)
        {
            CS2ArgumentsTextBox.Text = SettingsManager.CS2Arguments;
            bool IsSteamFolder = FileExistsIgnoreCase(SettingsManager.SteamPath, "Steam.exe");
            SteamPathBtn.ButtonCircleColor = IsSteamFolder ? Brushes.Green : Brushes.Red;
            string cs2Path = System.IO.Path.Combine(SettingsManager.CS2Path, "game", "bin", "win64");
            bool IsCS2Folder = FileExistsIgnoreCase(cs2Path, "cs2.exe");
            if (!IsSteamFolder)
            {
                MessageBox.Show("Steam Path is incorrect!!!");
                return;
            }
            if (!IsCS2Folder)
            {
                MessageBox.Show("CS2 Path is incorrect!!!");
                return;
            }
            List<AccountInstance> Accounts = AccountManager.GetSelectedAccounts(AccountListBox);

            new Thread(() => {
                foreach (var account in Accounts)
                {
                    if (account.MaFile == null)
                    {
                        MessageBox.Show($"{account.Login} MaFile NotFound.");
                        continue;
                    }
                    if (account.MaFile.Session == null)
                    {
                        MessageBox.Show($"{account.Login} MaFile Broken.");
                        continue;
                    }
                    if (account.MaFile.Session.SteamID == 0)
                    {
                        MessageBox.Show($"{account.Login} MaFile Broken.");
                        continue;
                    }
                    account.SteamClient.Start();
                }
            }).Start();

        }

        private void ButtonWIthTextOnly_ButtonClick_1(object sender, RoutedEventArgs e)
        {
            IntPtr hdc = Win32.GetDC(IntPtr.Zero);
            int screenWidth = Win32.GetDeviceCaps(hdc, Win32.DESKTOPHORZRES);
            int screenHeight = Win32.GetDeviceCaps(hdc, Win32.DESKTOPVERTRES);
            int x = 0;
            int y = 0;
            int maxHeightInRow = 0;
            if (Team1 != null)
            {
                Team1.Leader.CS2Client.CS2_WindowComponent.MoveCSWindow(0, 0);
                foreach (var acc in Team1.Bots)
                {
                    x += 383;
                    acc.CS2Client.CS2_WindowComponent.MoveCSWindow(x, 0);
                }
            }
            x = 0;
            if (Team2 != null)
            {
                Team2.Leader.CS2Client.CS2_WindowComponent.MoveCSWindow(0, 280);
                foreach (var acc in Team2.Bots)
                {
                    x += 383;
                    acc.CS2Client.CS2_WindowComponent.MoveCSWindow(x, 280);
                }
            }
            if (Team1 != null || Team2 != null)
            {
                return; 
            }
            foreach (var account in AccountManager.AccountList.Where(acc => acc.AccountStatus == Instances.EAccountStatus.InMainMenu))
            {
                if (Win32.GetWindowRect(account.CS2Client.CS2_WindowComponent.GetWindowHandle(), out Utils.RECT rect))
                {
                    int windowWidth = rect.Right - rect.Left;
                    int windowHeight = rect.Bottom - rect.Top;

                    if (x + windowWidth > screenWidth)
                    {
                        x = 0;
                        y += maxHeightInRow;
                        maxHeightInRow = 0;
                    }

                    account.CS2Client.CS2_WindowComponent.MoveCSWindow(x, y);
                    x += windowWidth;

                    if (windowHeight > maxHeightInRow)
                    {
                        maxHeightInRow = windowHeight;
                    }

                    if (y + windowHeight > screenHeight)
                    {
                        break;
                    }
                }
            }
        }
        private void ButtonWIthTextOnly_ButtonClick_2(object sender, RoutedEventArgs e)
        {
            ProcessesUtils.KillAllSteamAndCS();
        }
        private void ButtonWIthTextOnly_ButtonClick_3(object sender, RoutedEventArgs e)
        {
            
            List<AccountInstance> Accounts = AccountManager.GetSelectedAccounts(AccountListBox);

            new Thread(() => {
                foreach (var account in Accounts)
                {
                    account.SteamClient.Stop();
                }
            }).Start();
        }
        
        private void ButtonWIthTextOnly_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonWIthTextOnly_DisbanLobbies(object sender, RoutedEventArgs e)
        {
            ButtonWIthTextOnly button = (sender as ButtonWIthTextOnly);
            string OldText = button.ButtonText;
            button.IsEnabled = false;
            new Thread(() => {
                for (int i = 2; i >= 0; i--)
                {
                    Dispatcher.Invoke(() =>
                    {
                        button.ButtonText = i.ToString();
                    });

                    Thread.Sleep(1000);
                }
                Dispatcher.Invoke(() =>
                {
                    button.ButtonText = "Disban lobbies...";
                });

                if (Team1 != null)
                {
                    Team1.DisbanLobbies();
                }
                if (Team2 != null)
                {
                    Team2.DisbanLobbies();
                }
                Dispatcher.Invoke(() =>
                {
                    button.ButtonText = OldText;
                    button.IsEnabled = true;
                });

            }).Start();
            
           
        }
        private void ButtonWIthTextOnly_CollectLobbies(object sender, RoutedEventArgs e)
        {
            ButtonWIthTextOnly button = (sender as ButtonWIthTextOnly);
            string OldText = button.ButtonText;
            button.IsEnabled = false;
            new Thread(() => {
                for (int i = 5; i >= 0; i--)
                {
                    Dispatcher.Invoke(() =>
                    {
                        button.ButtonText = i.ToString();
                    });
                   
                    Thread.Sleep(1000);
                }
                Dispatcher.Invoke(() =>
                {
                    button.ButtonText = "Collecting...";
                });
               
                if (Team1 != null)
                {
                    Team1.CollectLobbies();
                }
                if (Team2 != null)
                {
                    Team2.CollectLobbies();
                }
                Dispatcher.Invoke(() =>
                {
                    button.ButtonText = OldText;
                    button.IsEnabled = true;
                });
               
            }).Start();

          
           
        }
        private void ButtonWIthTextOnly_ButtonClick_4(object sender, RoutedEventArgs e)
        {
            List<AccountInstance> Accounts = AccountManager.GetSelectedAccounts(AccountListBox);

            if (!(Accounts.Count == 2 || Accounts.Count == 4 || Accounts.Count == 5 || Accounts.Count == 10))
            {
                MessageBox.Show("You need 2,4,5 or 10 accounts.");
                return;
            }

            Accounts.Shuffle();

            if (Accounts.Count == 2 || Accounts.Count == 5)
            {
                Team1 = new LobbyInstance(Accounts[0], Accounts.GetRange(1, Accounts.Count - 1));
                Team2 = null;
                Lobby1LeaderLabel.Content = Team1.Leader.Login;
                Lobby1LeaderLabel.Visibility = Visibility.Visible;

                Lobby1BotsListBox.Items.Clear();
                foreach (var bot in Team1.Bots)
                {
                    Label accountlabel = new Label();
                    accountlabel.Padding = new Thickness(0, 0, 0, 0);
                    accountlabel.Foreground = Brushes.Green;
                    accountlabel.Content = $"{bot.Login}";
                    Lobby1BotsListBox.Items.Add(accountlabel);
                }

                Lobby2LeaderLabel.Visibility = Visibility.Hidden;
                Lobby2BotsListBox.Items.Clear();
            }
            else
            {
                int halfSize = Accounts.Count / 2;

                 Team1 = new LobbyInstance(Accounts[0], Accounts.GetRange(1, halfSize - 1));
                 Team2 = new LobbyInstance(Accounts[halfSize], Accounts.GetRange(halfSize + 1, Accounts.Count - halfSize - 1));

                Lobby1LeaderLabel.Content = Team1.Leader.Login;
                Lobby1LeaderLabel.Visibility = Visibility.Visible;
                Lobby1BotsListBox.Items.Clear();
                foreach (var bot in Team1.Bots)
                {
                    Label accountlabel = new Label();
                    accountlabel.Padding = new Thickness(0, 0, 0, 0);
                    accountlabel.Foreground = Brushes.Green;
                    accountlabel.Content = $"{bot.Login}";
                    Lobby1BotsListBox.Items.Add(accountlabel);
                }

                Lobby2LeaderLabel.Content = Team2.Leader.Login;
                Lobby2LeaderLabel.Visibility = Visibility.Visible;
                Lobby2BotsListBox.Items.Clear();
                foreach (var bot in Team2.Bots)
                {
                    Label accountlabel = new Label();
                    accountlabel.Padding = new Thickness(0, 0, 0, 0);
                    accountlabel.Foreground = Brushes.Green;
                    accountlabel.Content = $"{bot.Login}";
                    Lobby2BotsListBox.Items.Add(accountlabel);
                }
            }

        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://7vb.store/");
        }

        private void Image_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://t.me/EclipseFarm");
        }

        private void Image_MouseLeftButtonDown_2(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.com/invite/UGYp4zZ9Ks");
        }
    }
}
