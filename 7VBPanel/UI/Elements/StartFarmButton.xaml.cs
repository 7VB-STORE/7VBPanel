using _7VBPanel.Instances;
using _7VBPanel.Managers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _7VBPanel.UI.Elements
{
    /// <summary>
    /// Interaction logic for StartFarmButton.xaml
    /// </summary>
    public partial class StartFarmButton : UserControl
    {
        private bool isRunning = false;

        /// <summary>Выбранные аккаунты для фарма (задаёт MainWindow).</summary>
        public Func<List<AccountInstance>> GetAccountsFunc { get; set; }

        public StartFarmButton()
        {
            InitializeComponent();
            FarmManager.OnFarmEnded += OnFarmEnded;
        }

        private void OnFarmEnded()
        {
            Dispatcher.Invoke(() =>
            {
                if (!isRunning) return;
                SetUiRunning(false);
            });
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (isRunning)
                Stop();
            else
                Start();
        }

        private void Start()
        {
            var accounts = GetAccountsFunc?.Invoke();
            if (accounts == null || accounts.Count == 0)
            {
                MessageBox.Show("Выберите аккаунты и запустите CS2 (Start).");
                return;
            }

            FarmManager.Start(accounts);
            if (!FarmManager.IsRunning)
                return;

            SetUiRunning(true);
        }

        private void Stop()
        {
            FarmManager.Stop();
            SetUiRunning(false);
        }

        private void SetUiRunning(bool running)
        {
            isRunning = running;
            if (running)
            {
                imgIcon.Source = new BitmapImage(new Uri("/Resources/Icons/Stop.png", UriKind.Relative));
                txtLabel.Text = "Stop Farm";
                btnStartStop.Background = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                btnStartStop.Tag = "Running";
            }
            else
            {
                imgIcon.Source = new BitmapImage(new Uri("/Resources/Icons/Play.png", UriKind.Relative));
                txtLabel.Text = "Start Farm";
                btnStartStop.Background = Brushes.Transparent;
                btnStartStop.Tag = "Stopped";
            }
        }
    }
}
