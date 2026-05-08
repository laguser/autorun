using System.Windows;

namespace autorun
{
    public partial class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmDialog(string title, string message, string yesText, Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
            TitleText.Text = title;
            MessageText.Text = message;
            if (!string.IsNullOrEmpty(yesText))
                YesButton.Content = yesText;
            Confirmed = false;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            this.Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            this.Close();
        }

        public static bool Show(string title, string message, string yesText, Window owner)
        {
            ConfirmDialog dlg = new ConfirmDialog(title, message, yesText, owner);
            dlg.ShowDialog();
            return dlg.Confirmed;
        }
    }
}
