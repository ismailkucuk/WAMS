using System.Windows;

namespace wam.Dialogs
{
    public partial class AdminRequiredDialog : Window
    {
        public AdminRequiredDialog()
        {
            InitializeComponent();
        }

        private void GoSettings_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}


