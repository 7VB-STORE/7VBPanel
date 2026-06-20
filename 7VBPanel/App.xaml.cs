using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using _7VBPanel.Utils;

namespace _7VBPanel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Win32.ShowConsole();
            PanelLog.Line("7VBPanel запущен. Лог дублируется в файл 7VBPanel.log рядом с .exe");
            Console.WriteLine("Консоль: все Console.WriteLine и PanelLog.Line видны здесь и в 7VBPanel.log");
            base.OnStartup(e);
        }
    }
}
