using System.Windows.Controls;
using wam.Services;

namespace wam.Pages
{
    public partial class ProcessMonitorPage : UserControl
    {
        public ProcessMonitorPage()
        {
            InitializeComponent();

            var data = ProcessService.GetProcesses();
            ProcessListView.ItemsSource = data;
        }
    }
}
