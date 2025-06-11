using System.Windows;
using System.Windows.Controls;
using wam.Services;

namespace wam.Pages
{
    public partial class NetworkMonitorPage : UserControl
    {
        public NetworkMonitorPage()
        {
            InitializeComponent();
            LoadAllConnections();
        }

        private void LoadAllConnections()
        {
            ConnectionListView.ItemsSource = AdvancedNetworkService.GetAllConnections();
        }

        private void LoadListening(object sender, RoutedEventArgs e)
        {
            ConnectionListView.ItemsSource = AdvancedNetworkService.GetAllConnections(onlyListening: true);
        }

        private void LoadCritical(object sender, RoutedEventArgs e)
        {
            ConnectionListView.ItemsSource = AdvancedNetworkService.GetAllConnections(onlyCritical: true);
        }

        private void LoadAll(object sender, RoutedEventArgs e)
        {
            LoadAllConnections();
        }
        private void BlockPort_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int port))
            {
                var result = MessageBox.Show($"Port {port} için erişimi kapatmak istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    PortManager.BlockPort(port);
                }
            }
        }
        private void UnblockPort_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int port))
            {
                var result = MessageBox.Show($"Port {port} için engellemeyi kaldırmak istiyor musunuz?", "Onay", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    PortManager.UnblockPort(port);
                }
            }
        }


    }
}
