using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace Verdant.KartRider
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NaverAccount account;
        private KartGame Kart;

        public MainWindow(NaverAccount acc)
        {
            InitializeComponent();
            account = acc;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Process.GetProcessesByName("KartRider").Length > 0
             || Process.GetProcessesByName("NGM").Length > 0)
            {
                var mbr = MessageBox.Show("You are already playing Kart Rider! Continue?", "Kart", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (mbr == MessageBoxResult.No)
                {
                    Close();
                    return;
                }
            }

            Kart = new KartGame(account);

            try
            {
                await Kart.Init();
            }
            catch (VerdantException.GameNotFoundException)
            {
                MessageBox.Show("Looks like we couldn't find your installation of Kart Rider! Make sure you have Korean Kart Rider + NGM installed properly!");
                Close();
                return;
            }
            catch (VerdantException.ChannelingRequiredException)
            {
                statusLabel.Content = "Loading... (channeling)";
                try
                {
                    await Kart.Channel();
                }
                catch
                {
                    MessageBox.Show("Error channeling with Nexon and your Naver account. Please try again later.");
                    Close();
                    return;
                }
            }
            catch
            {
                MessageBox.Show("Error connecting to Kart Rider, please try again later.");
                Close();
                return;
            }

            statusLabel.Content = "Logged in as: " + Kart.MainCharName;
            startButton.IsEnabled = true;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;
            try
            {
                await Kart.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting game...\n\n" + ex.ToString());
            }
            Close();
        }
    }
}
