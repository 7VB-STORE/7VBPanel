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
    /// Interaction logic for SelectPathButton.xaml
    /// </summary>
    public partial class SelectPathButton : UserControl
    {
        public SelectPathButton()
        {
            InitializeComponent();
            DataContext = this;
        }
        public static readonly DependencyProperty ButtonTextProperty =
         DependencyProperty.Register("ButtonText", typeof(string), typeof(SelectPathButton), new PropertyMetadata("Settings"));

        public static readonly DependencyProperty ButtonCircleColorProperty =
        DependencyProperty.Register("ButtonCircleColor", typeof(Brush), typeof(SelectPathButton), new PropertyMetadata(Brushes.Transparent));

        public static readonly RoutedEvent ButtonClickEvent =
            EventManager.RegisterRoutedEvent("ButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectPathButton));

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public Brush ButtonCircleColor
        {
            get { return (Brush)GetValue(ButtonCircleColorProperty); }
            set { SetValue(ButtonCircleColorProperty, value); }
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
