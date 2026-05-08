using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace autorun
{
    public partial class AboutDialog : Window
    {
        public AboutDialog(Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Telegram_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://t.me/lagusertg");
        }

        private void YouTube_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://youtu.be/dQw4w9WgXcQ?list=RDdQw4w9WgXcQ");
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/laguser");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        public static void Show(Window owner)
        {
            AboutDialog dlg = new AboutDialog(owner);
            dlg.ShowDialog();
        }
    }
}
