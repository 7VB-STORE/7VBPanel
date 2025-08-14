using System;
using System.Collections.Generic;
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

namespace _7VBPanel.UI.Elements
{
    /// <summary>
    /// Interaction logic for StartFarmButton.xaml
    /// </summary>
    public partial class StartFarmButton : UserControl
    {
        private bool isRunning = false;
        public StartFarmButton()
        {
            InitializeComponent();
            /*FarmManager.OnFarmEnded += ()=>{
                isRunning = false;
                imgIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Resources/Icons/Play.png", UriKind.Relative));
                txtLabel.Text = "Start Farm";
                btnStartStop.Background = Brushes.Transparent;
                btnStartStop.Tag = "Stopped";
            };*/
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }
        private void Start()
        {
            isRunning = true;
            imgIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Resources/Icons/Stop.png", UriKind.Relative));
            txtLabel.Text = "Stop Farm";
            btnStartStop.Background = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
            btnStartStop.Tag = "Running";
           /* PanelMainManager.SetStatus("Starting Farm");
            FarmManager.Start();*/
        }

        private void Stop()
        {
            isRunning = false;
            imgIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Resources/Icons/Play.png", UriKind.Relative));
            txtLabel.Text = "Start Farm";
            btnStartStop.Background = Brushes.Transparent;
            btnStartStop.Tag = "Stopped";
/*            PanelMainManager.SetStatus("IDLE");
            FarmManager.Stop();*/
        }
    }
}
