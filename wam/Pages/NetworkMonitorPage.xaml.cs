using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using wam.Services;

namespace wam.Pages
{
    public partial class NetworkMonitorPage : UserControl, ILoadablePage
    {
        public event Action<bool, string> LoadingStateChanged;
        public ObservableCollection<NetworkConnectionViewModel> Connections { get; set; }
        private string _currentFilter = "All";

        public NetworkMonitorPage()
        {
            InitializeComponent();
            Connections = new ObservableCollection<NetworkConnectionViewModel>();
            ConnectionListView.ItemsSource = Connections;

            // Export control'ü bu sayfaya bağla
            ExportControl.TargetPage = this;

            // Checked event'leri LoadConnectionsAsync'i tetikleyecek.
            AllConnectionsRadio.Checked += Filter_Checked;
            ListeningPortsRadio.Checked += Filter_Checked;
            CriticalPortsRadio.Checked += Filter_Checked;
        }

        public async Task LoadDataAsync()
        {
            await LoadConnectionsAsync(_currentFilter);
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = Connections.Select(c => new
            {
                ProcessId = c.ProcessId,
                ProcessName = c.ProcessName,
                LocalAddress = c.LocalAddress,
                LocalPort = c.LocalPort,
                RemoteAddress = c.RemoteAddress,
                RemoteDomain = c.RemoteDomain,
                RemotePort = c.RemotePort,
                State = c.State,
                Protocol = c.Protocol,
                RiskLabel = c.RiskLabel,
                IsRisky = c.IsRisky,
                IsBlocked = c.IsBlocked
            }).ToList();

            ExportService.ExportToJson(exportData, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = Connections.Select(c => new
            {
                ProcessId = c.ProcessId,
                ProcessName = c.ProcessName,
                LocalAddress = c.LocalAddress,
                LocalPort = c.LocalPort,
                RemoteAddress = c.RemoteAddress,
                RemoteDomain = c.RemoteDomain,
                RemotePort = c.RemotePort,
                State = c.State,
                Protocol = c.Protocol,
                RiskLabel = c.RiskLabel,
                IsRisky = c.IsRisky,
                IsBlocked = c.IsBlocked,
                FilterApplied = _currentFilter
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = Connections.Select(c => new
            {
                ProcessId = c.ProcessId,
                ProcessName = c.ProcessName,
                LocalAddress = c.LocalAddress,
                LocalPort = c.LocalPort,
                RemoteAddress = c.RemoteAddress,
                RemoteDomain = c.RemoteDomain,
                RemotePort = c.RemotePort,
                State = c.State,
                Protocol = c.Protocol,
                RiskLabel = c.RiskLabel,
                IsRisky = c.IsRisky,
                IsBlocked = c.IsBlocked
            }).ToList();

            ExportService.AutoExport(exportData, GetModuleName());
        }

        public string GetModuleName()
        {
            return "NetworkMonitor";
        }

        private async void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            if (sender is RadioButton radioButton && radioButton.IsChecked == true)
            {
                string filter = radioButton.Tag?.ToString() ?? "All";
                _currentFilter = filter;
                await LoadConnectionsAsync(filter);
            }
        }

        private async Task LoadConnectionsAsync(string filter)
        {
            LoadingStateChanged?.Invoke(true, "Ağ bağlantıları taranıyor...");

            List<NetworkConnectionViewModel> connectionListViewModels = await Task.Run(() =>
            {
                // HATA DÜZELTME: 'AdvancedNetworkSertvice' yazım hatası düzeltildi.
                var serviceData = filter switch
                {
                    "Listening" => AdvancedNetworkService.GetAllConnections(onlyListening: true),
                    "Critical" => AdvancedNetworkService.GetAllConnections(onlyCritical: true),
                    _ => AdvancedNetworkService.GetAllConnections(),
                };

                return serviceData.Select(conn => new NetworkConnectionViewModel
                {
                    ProcessId = conn.ProcessId,
                    ProcessName = conn.ProcessName,
                    LocalAddress = conn.LocalAddress,
                    LocalPort = conn.LocalPort,
                    RemoteAddress = conn.RemoteAddress,
                    RemoteDomain = conn.RemoteDomain,
                    RemotePort = conn.RemotePort,
                    State = conn.State,
                    Protocol = conn.Protocol,
                    RiskLabel = conn.RiskLabel,
                    IsBlocked = false
                }).ToList();
            });

            Connections.Clear();
            foreach (var vm in connectionListViewModels)
            {
                Connections.Add(vm);
            }

            LoadingStateChanged?.Invoke(false, null);
        }

        private void BlockPort_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is NetworkConnectionViewModel vm)
            {
                var result = MessageBox.Show($"Port {vm.LocalPort} için GİDEN ve GELEN tüm bağlantıları Windows Güvenlik Duvarı'nda engellemek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    PortManager.BlockPort(vm.LocalPort);
                    vm.IsBlocked = true;
                }
            }
        }

        private void UnblockPort_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is NetworkConnectionViewModel vm)
            {
                var result = MessageBox.Show($"Port {vm.LocalPort} için engellemeyi kaldırmak istiyor musunuz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    PortManager.UnblockPort(vm.LocalPort);
                    vm.IsBlocked = false;
                }
            }
        }
    }

    public class NetworkConnectionViewModel : INotifyPropertyChanged
    {
        private bool _isBlocked;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string LocalAddress { get; set; }
        public int LocalPort { get; set; }
        public string RemoteAddress { get; set; }
        public string RemoteDomain { get; set; }
        public int RemotePort { get; set; }
        public string State { get; set; }
        public string Protocol { get; set; }
        public string RiskLabel { get; set; }

        public bool IsRisky => !string.IsNullOrEmpty(RiskLabel);

        public bool IsBlocked
        {
            get => _isBlocked;
            set { if (_isBlocked != value) { _isBlocked = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}