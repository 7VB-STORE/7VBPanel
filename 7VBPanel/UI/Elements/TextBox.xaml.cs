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
    /// Interaction logic for TextBox.xaml
    /// </summary>
    public partial class TextBox : UserControl
    {
        public TextBox()
        {
            InitializeComponent();
            UpdatePlaceholderVisibility();
        }
        public static readonly RoutedEvent TextChangedEvent =
            EventManager.RegisterRoutedEvent("Text_Changed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TextBox));

        public static readonly DependencyProperty BroderRadiusProperty =
         DependencyProperty.Register("BorderRadius", typeof(int), typeof(TextBox), new PropertyMetadata(8));
        public int BorderRadius
        {
            get { return (int)GetValue(BroderRadiusProperty); }
            set { SetValue(BroderRadiusProperty, value); }
        }

        public double TextSize
        {
            get { return textBox.FontSize; }
            set { textBox.FontSize = value;
                placeholderText.FontSize = value;            
            }
        }
        public event RoutedEventHandler Text_Changed
        {
            add { AddHandler(TextChangedEvent, value); }
            remove { RemoveHandler(TextChangedEvent, value); }
        }
        public string Text
        {
            get { return textBox.Text; }
            set { textBox.Text = value; UpdatePlaceholderVisibility(); }
        }

        public string Placeholder
        {
            get { return placeholderText.Text; }
            set { placeholderText.Text = value; }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholderVisibility();
            RaiseEvent(new RoutedEventArgs(TextChangedEvent));
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void UpdatePlaceholderVisibility()
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                placeholderText.Visibility = textBox.IsFocused ? Visibility.Collapsed : Visibility.Visible;
                textBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                placeholderText.Visibility = Visibility.Collapsed;
                textBox.Foreground = new SolidColorBrush(Colors.White);
            }
        }
    }

}
