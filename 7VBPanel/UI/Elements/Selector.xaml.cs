using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for Selector.xaml
    /// </summary>
    public partial class Selector : UserControl
    {
        public Selector()
        {
            InitializeComponent();
            Items = new ObservableCollection<string>();
            DataContext = this;
        }
        public static readonly DependencyProperty ItemsProperty =
           DependencyProperty.Register("Items", typeof(ObservableCollection<string>), typeof(Selector), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnItemsPropertyChanged));
        public static readonly RoutedEvent Text_ChangedEvent =
         EventManager.RegisterRoutedEvent("Text_Changed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Selector));

        public static readonly DependencyProperty SelectorTextProperty =
        DependencyProperty.Register("SelectorText", typeof(string), typeof(Selector), new PropertyMetadata("Name"));
        public string SelectorText
        {
            get { return (string)GetValue(SelectorTextProperty); }
            set { SetValue(SelectorTextProperty, value); }
        }

        public event RoutedEventHandler Text_Changed
        {
            add { AddHandler(Text_ChangedEvent, value); }
            remove { RemoveHandler(Text_ChangedEvent, value); }
        }
        public ObservableCollection<string> Items
        {
            get { return (ObservableCollection<string>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }
        public string SelectedText;
        private static void OnItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Selector control)
            {
                control.ComboBox_DefaultTab.ItemsSource = e.NewValue as ObservableCollection<string>;
            }
        }

        private void InvokeCommandAction_Changed(object sender, EventArgs e)
        {

        }
        public string GetText()
        {
            return SelectedText;
        }
        public void SetText(string Text)
        {
            ComboBox_DefaultTab.Text = Text;
        }
        private void ComboBox_DefaultTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboBox_DefaultTab_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

            if (sender is System.Windows.Controls.ComboBox comboBox)
            {
                if (comboBox.SelectedItem != null)
                {
                    SelectedText = comboBox.SelectedItem.ToString();
                    RaiseEvent(new RoutedEventArgs(Text_ChangedEvent));
                }
            }
        }
    }
}
