using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using wam.Services; // Servislerinizin olduğu namespace (varsayım)

namespace wam.Pages
{
    public partial class DashboardPage : UserControl, ILoadablePage, IDisposable
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage()
        {
            InitializeComponent();
            _viewModel = new DashboardViewModel();
            this.DataContext = _viewModel;
            
            // Sayfadan ayrılırken timer gibi kaynakları temizlemek için
            this.Unloaded += UserControl_Unloaded;
        }

        public async Task LoadDataAsync()
        {
            await _viewModel.LoadInitialDataAsync();
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = new
            {
                SystemMetrics = new
                {
                    CpuUsage = _viewModel.CpuUsage,
                    UsedRamGB = _viewModel.UsedRamGB,
                    TotalRamGB = _viewModel.TotalRamGB,
                    Uptime = _viewModel.Uptime,
                    ActiveConnections = _viewModel.ActiveConnections,
                    ListeningPorts = _viewModel.ListeningPorts,
                    SecurityWarningsCount = _viewModel.SecurityWarningsCount
                },
                RecentActivities = _viewModel.RecentActivities.Select(a => new
                {
                    Description = a.Description,
                    Time = a.Time,
                    Icon = a.Icon
                }).ToList()
            };

            ExportService.ExportToJson(new[] { exportData }, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = new List<dynamic>();
            
            // Sistem metrikleri için bir satır
            csvData.Add(new
            {
                Type = "System Metrics",
                CpuUsage = _viewModel.CpuUsage,
                UsedRamGB = _viewModel.UsedRamGB,
                TotalRamGB = _viewModel.TotalRamGB,
                Uptime = _viewModel.Uptime,
                ActiveConnections = _viewModel.ActiveConnections,
                ListeningPorts = _viewModel.ListeningPorts,
                SecurityWarningsCount = _viewModel.SecurityWarningsCount
            });

            // Aktiviteler için satırlar
            foreach (var activity in _viewModel.RecentActivities)
            {
                csvData.Add(new
                {
                    Type = "Activity",
                    Description = activity.Description,
                    Time = activity.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    Icon = activity.Icon,
                    CpuUsage = "",
                    UsedRamGB = "",
                    TotalRamGB = "",
                    Uptime = "",
                    ActiveConnections = "",
                    ListeningPorts = "",
                    SecurityWarningsCount = ""
                });
            }

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = new
            {
                SystemMetrics = new
                {
                    CpuUsage = _viewModel.CpuUsage,
                    UsedRamGB = _viewModel.UsedRamGB,
                    TotalRamGB = _viewModel.TotalRamGB,
                    Uptime = _viewModel.Uptime,
                    ActiveConnections = _viewModel.ActiveConnections,
                    ListeningPorts = _viewModel.ListeningPorts,
                    SecurityWarningsCount = _viewModel.SecurityWarningsCount
                },
                RecentActivities = _viewModel.RecentActivities.Select(a => new
                {
                    Description = a.Description,
                    Time = a.Time,
                    Icon = a.Icon
                }).ToList()
            };

            ExportService.AutoExport(new[] { exportData }, GetModuleName());
        }

        public string GetModuleName()
        {
            return "Dashboard";
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string pageName)
            {
                // Gerçek navigasyon için MainWindow'a bir olay (event) gönderilebilir.
                MessageBox.Show($"{pageName} sayfasına yönlendirilecek.");
            }
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
    }

    // --- VIEWMODEL VE YARDIMCI SINIFLAR ---

    public class DashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DispatcherTimer _liveDataTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;

        private int _cpuUsage;
        public int CpuUsage { get => _cpuUsage; set { _cpuUsage = value; OnPropertyChanged(); } }

        private double _usedRamGB;
        public double UsedRamGB { get => _usedRamGB; set { _usedRamGB = value; OnPropertyChanged(); } }

        public double TotalRamGB { get; }

        private string _uptime;
        public string Uptime { get => _uptime; private set { _uptime = value; OnPropertyChanged(); } }

        private int _activeConnections;
        public int ActiveConnections { get => _activeConnections; set { _activeConnections = value; OnPropertyChanged(); } }

        private int _listeningPorts;
        public int ListeningPorts { get => _listeningPorts; set { _listeningPorts = value; OnPropertyChanged(); } }

        private int _securityWarningsCount;
        public int SecurityWarningsCount { get => _securityWarningsCount; set { _securityWarningsCount = value; OnPropertyChanged(); } }

        public ObservableCollection<ActivityItem> RecentActivities { get; } = new ObservableCollection<ActivityItem>();

        public DashboardViewModel()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

                var wmiObject = new ManagementObjectSearcher("select * from Win32_ComputerSystem").Get().Cast<ManagementObject>().First();
                TotalRamGB = Math.Round(Convert.ToDouble(wmiObject["TotalPhysicalMemory"]) / (1024 * 1024 * 1024), 1);

                _liveDataTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                _liveDataTimer.Tick += UpdateLiveMetrics;
                _liveDataTimer.Start();
                UpdateLiveMetrics(null, null); // İlk değerleri hemen al
            }
            catch (Exception ex)
            {
                // Performans sayaçları başlatılamazsa hata mesajı göster
                Uptime = "Performans sayaçları okunamadı.";
                Debug.WriteLine($"Dashboard ViewModel Error: {ex.Message}");
            }
        }

        private void UpdateLiveMetrics(object sender, EventArgs e)
        {
            try
            {
                CpuUsage = (int)_cpuCounter.NextValue();
                UsedRamGB = TotalRamGB - (_ramCounter.NextValue() / 1024);

                TimeSpan uptimeSpan = TimeSpan.FromMilliseconds(Environment.TickCount64);
                Uptime = $"{(int)uptimeSpan.TotalDays} gün, {uptimeSpan.Hours} saat";
            }
            catch { /* Hataları yoksay */ }
        }

        public async Task LoadInitialDataAsync()
        {
            // Ağır işlemleri arka planda yapıp sonuçları döndür
            var (connections, activities, warnings) = await Task.Run(() =>
            {
                var connList = AdvancedNetworkService.GetAllConnections();
                var activityList = UserActivityService.GetLoginLogoutEvents(5);
                int warningCount = 3; // Örnek değer
                return (connList, activityList, warningCount);
            });

            // UI thread'inde koleksiyonları ve özellikleri güncelle
            ActiveConnections = connections.Count(c => c.State == "Established");
            ListeningPorts = connections.Count(c => c.State == "Listen");
            SecurityWarningsCount = warnings;

            RecentActivities.Clear();
            foreach (var activity in activities)
            {
                RecentActivities.Add(new ActivityItem
                {
                    Description = $"{activity.UserName} kullanıcısı {activity.EventType}",
                    Time = activity.TimeCreated,
                    Icon = activity.EventType.Contains("Açıldı") ? "\uE836" : "\uE777",
                    IconColor = activity.EventType.Contains("Açıldı") ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red)
                });
            }
        }

        public void Dispose()
        {
            _liveDataTimer?.Stop();
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ActivityItem
    {
        public string Icon { get; set; }
        public Brush IconColor { get; set; }
        public string Description { get; set; }
        public DateTime Time { get; set; }
    }

    // XAML'deki dairesel gösterge için Converter
    public class CircularProgressBarConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || !(values[0] is int) && !(values[0] is double) || !(values[1] is double) || !(values[2] is double))
                return "0";

            double value = System.Convert.ToDouble(values[0]);
            double maximum = (double)values[1];
            double width = (double)values[2];

            if (width == 0 || maximum == 0) return "0";

            double circumference = Math.PI * width;
            double progress = (value / maximum) * circumference;
            // Değer maksimumu aşarsa tam daire göster
            if (progress > circumference) progress = circumference;

            double remaining = circumference - progress;

            // Negatif değerleri engelle
            if (progress < 0) progress = 0;
            if (remaining < 0) remaining = circumference;

            return $"{progress} {remaining}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
