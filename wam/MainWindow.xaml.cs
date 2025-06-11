using System.Windows;
using System.Windows.Controls;
using wam.Pages; // sayfa klasörünü tanıttık

namespace wam
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadUserActivityPage()
        {
            ContentPanel.Children.Clear(); // eski içeriği temizle
            UserActivityPage page = new UserActivityPage();
            ContentPanel.Children.Add(page); // yeni içeriği ekle
        }

        // Bu methodları butonlara bağlayacağız
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new TextBlock
            {
                Text = "Dashboard görünümü burada olacak.",
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        private void UserActivity_Click(object sender, RoutedEventArgs e)
        {
            LoadUserActivityPage();
        }

        private void StartupPrograms_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new StartupProgramsPage());
        }
        private void ProcessMonitor_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new ProcessMonitorPage());
        }

        private void NetworkMonitor_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new NetworkMonitorPage());
        }

        private void FileSystemMonitor_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new FileSystemMonitorPage());
        }

        private void UsbMonitor_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new UsbMonitorPage());
        }

        private void InstalledSoftware_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new InstalledSoftwarePage());
        }

        private void SystemInfo_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new SystemInfoPage());
        }

        private void UserSessionInfo_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new UserSessionInfoPage());
        }
        private void SecurityPolicy_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new SecurityPolicyPage());
        }

        private void ActiveAppMonitorMenu_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new ActiveAppMonitorPage());
        }

        private void EventLogAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new EventLogAnalyzerPage());
        }

        private void AnomalyDetection_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(new AnomalyDetectionPage());
        }
    }
}
