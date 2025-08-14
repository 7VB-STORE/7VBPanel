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
    /// Interaction logic for Button.xaml
    /// </summary>
    public partial class Button : UserControl
    {
        public static readonly DependencyProperty ButtonTextProperty =
           DependencyProperty.Register("ButtonText", typeof(string), typeof(Button), new PropertyMetadata("Settings"));

        public static readonly DependencyProperty ButtonIconProperty =
            DependencyProperty.Register("ButtonIcon", typeof(string), typeof(Button), new PropertyMetadata(string.Empty));

        public static readonly RoutedEvent ButtonClickEvent =
            EventManager.RegisterRoutedEvent("ButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Button));

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public string ButtonIcon
        {
            get { return (string)GetValue(ButtonIconProperty); }
            set { SetValue(ButtonIconProperty, value); }
        }

        public event RoutedEventHandler ButtonClick
        {
            add { AddHandler(ButtonClickEvent, value); }
            remove { RemoveHandler(ButtonClickEvent, value); }
        }

        public Button()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ButtonClickEvent));
        }
    }
}
