using System.Windows;

namespace wam.Pages
{
    public partial class AddUserWindow : Window
    {
        public string NewUserName => UserNameBox.Text.Trim();
        public string NewPassword => PasswordBox.Password;

        public AddUserWindow()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewUserName) || string.IsNullOrWhiteSpace(NewPassword))
            {
                MessageBox.Show("Kullanıcı adı ve parola boş olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
