using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace _7VBPanel.UI.Elements
{
    public partial class ToggleSelection : UserControl
    {
        public ToggleSelection()
        {
            InitializeComponent();
            DataContext = this;
        }
        public static readonly RoutedEvent OnCheckedEvent =
           EventManager.RegisterRoutedEvent("OnChecked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleSelection));
        public static readonly DependencyProperty ToggleTextProperty =
            DependencyProperty.Register("ToggleText", typeof(string), typeof(ToggleSelection), new PropertyMetadata("Login1"));

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register("TextColor", typeof(Brush), typeof(ToggleSelection), new PropertyMetadata(Brushes.White));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ToggleSelection), new PropertyMetadata(false));

        public static readonly DependencyProperty ToggleBackgroundProperty =
            DependencyProperty.Register("ToggleBackground", typeof(Brush), typeof(ToggleSelection), new PropertyMetadata(Brushes.Red));

        public event RoutedEventHandler Checked
        {
            add { AddHandler(OnCheckedEvent, value); }
            remove { RemoveHandler(OnCheckedEvent, value); }
        }
        public string ToggleText
        {
            get { return (string)GetValue(ToggleTextProperty); }
            set { SetValue(ToggleTextProperty, value); }
        }

        public Brush TextColor
        {
            get { return (Brush)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public Brush ToggleBackground
        {
            get { return (Brush)GetValue(ToggleBackgroundProperty); }
            set { SetValue(ToggleBackgroundProperty, value); }
        }

        private void textBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            toggleButton.IsChecked = !toggleButton.IsChecked;
            RaiseEvent(new RoutedEventArgs(OnCheckedEvent));
        }
    }
}
