using System.Windows.Controls;
using wam.Services;

namespace wam.Pages
{
    public partial class StartupProgramsPage : UserControl
    {
        public StartupProgramsPage()
        {
            InitializeComponent();

            var data = StartupProgramsService.GetStartupPrograms();
            StartupListView.ItemsSource = data;
        }
    }
}
