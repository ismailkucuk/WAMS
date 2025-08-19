using System.Windows;

namespace wam.Dialogs
{
    public partial class CloseConfirmDialog : Window
    {
        public bool MinimizeSelected => MinimizeOption.IsChecked == true;
        public bool CloseSelected => CloseOption.IsChecked == true;
        public bool AlwaysMinimize => RememberOption.IsChecked == true;

        public CloseConfirmDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

