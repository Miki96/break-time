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
using StackExchange.Redis;
using System.IO;
using System.Windows.Threading;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // game manager
        Manager manager;

        public MainWindow()
        {
            InitializeComponent();
            // create manager
            manager = Manager.getInstance();
            // set reference to label
            manager.window = this;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            manager.startServer();
        }

        public void updateText(String txt)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                info.Text += txt + "\n";
            }), DispatcherPriority.Background);
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Autoscroll
            if (e.ExtentHeightChange != 0)
            {
                scroll.ScrollToVerticalOffset(scroll.ExtentHeight);
            }
        }
    }
}
