using System.Windows.Controls;
using wam.Services;

namespace wam.Pages
{
    public partial class UserActivityPage : UserControl
    {
        public UserActivityPage()
        {
            InitializeComponent();

            // Event log verilerini al
            var events = UserActivityService.GetLoginLogoutEvents();
            ActivityListView.ItemsSource = events;
        }
    }
}
