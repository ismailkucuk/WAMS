using System.Windows;

namespace wam.Pages
{
    public partial class UserActivityDetailWindow : Window
    {
        public UserActivityDetailWindow(UserActivityViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}
