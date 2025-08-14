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
    /// Interaction logic for ButtonWIthTextOnly.xaml
    /// </summary>
    public partial class ButtonWIthTextOnly : UserControl
    {
        public ButtonWIthTextOnly()
        {
            InitializeComponent();
            DataContext = this;
        }
       
        public static readonly DependencyProperty ButtonBroderRadiusProperty =
         DependencyProperty.Register("ButtonBorderRadius", typeof(int), typeof(ButtonWIthTextOnly), new PropertyMetadata(8));

        public static readonly DependencyProperty ButtonBackgroundColorProperty =
            DependencyProperty.Register("ButtonBackgroundColor", typeof(Brush), typeof(ButtonWIthTextOnly), new PropertyMetadata(Brushes.Transparent));

        public Brush ButtonBackgroundColor
        {
            get { return (Brush)GetValue(ButtonBackgroundColorProperty); }
            set { SetValue(ButtonBackgroundColorProperty, value); }
        }
        public int ButtonBorderRadius
        {
            get { return (int)GetValue(ButtonBroderRadiusProperty); }
            set { SetValue(ButtonBroderRadiusProperty, value); }
        }

        public static readonly RoutedEvent ButtonClickEvent =
            EventManager.RegisterRoutedEvent("ButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ButtonWIthTextOnly));
        public static readonly DependencyProperty ButtonTextProperty =
        DependencyProperty.Register("ButtonText", typeof(string), typeof(ButtonWIthTextOnly), new PropertyMetadata("Settings"));
        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }
      

        public event RoutedEventHandler ButtonClick
        {
            add { AddHandler(ButtonClickEvent, value); }
            remove { RemoveHandler(ButtonClickEvent, value); }
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ButtonClickEvent));
        }
    }
}
