using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using wam.Pages;

namespace wam
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Açılışta Dashboard'u veya boş bir başlangıç sayfası yükleyelim
            // Şimdilik boş bırakıyoruz, ilk tıklama ile sayfa yüklenecek.
            ContentTitle.Text = "Windows Activity Monitor";
        }

        // BÜTÜN SAYFA GEÇİŞ MANTIĞINI YÖNETEN MERKEZİ VE SAĞLAM METOT
        private async Task NavigateToPage<T>(string title) where T : UserControl, new()
        {
            // Önceki sayfadaki olay aboneliğini kaldır
            if (PageHost.Content is EventLogAnalyzerPage oldEventPage) { oldEventPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is UserActivityPage oldUserActivityPage) { oldUserActivityPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is UserSessionInfoPage oldUserSessionPage) { oldUserSessionPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is SecurityPolicyPage oldSecurityPage) { oldSecurityPage.LoadingStateChanged -= OnPageLoadingStateChanged; }

            // 1. Yükleme ekranını göster
            LoadingOverlay.Visibility = Visibility.Visible;
            ContentTitle.Text = title;
            await Task.Delay(50); // Arayüzün yükleme ekranını çizmesine izin ver

            try
            {
                // 2. Yeni sayfayı oluştur
                var page = new T();

                // 3. Eğer sayfanın özel bir "Yükleniyor" olayı varsa, onu dinlemeye başla
                // Yeni sayfa için olay aboneliği yap
                if (page is EventLogAnalyzerPage newEventPage) { newEventPage.LoadingStateChanged += OnPageLoadingStateChanged; }
                if (page is UserActivityPage newUserActivityPage) { newUserActivityPage.LoadingStateChanged += OnPageLoadingStateChanged; }
                if (page is UserSessionInfoPage newUserSessionPage) { newUserSessionPage.LoadingStateChanged += OnPageLoadingStateChanged; }
                if (page is SecurityPolicyPage newSecurityPage) { newSecurityPage.LoadingStateChanged += OnPageLoadingStateChanged; }


                // 4. Eğer sayfa asenkron yükleme gerektiriyorsa, bekle
                if (page is ILoadablePage loadablePage)
                {
                    await loadablePage.LoadDataAsync();
                }

                // 5. Her şey yolundaysa, sayfayı ekrana yerleştir
                PageHost.Content = page;
            }
            catch (Exception ex)
            {
                // 6. Bir hata olursa, kullanıcıya göster ve içeriği temizle
                MessageBox.Show($"Sayfa yüklenirken bir hata oluştu:\n\n{ex.Message}", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                PageHost.Content = null;
            }
            finally
            {
                // 7. Hata olsa da olmasa da, Yükleme ekranını HER ZAMAN gizle
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // Sayfalardan gelen "Yükleniyor..." sinyallerini işleyen metot
        private void OnPageLoadingStateChanged(bool isLoading, string message)
        {
            // İsteğe bağlı: Yükleme mesajını dinamik olarak değiştirmek için
            // MainWindow.xaml'daki LoadingOverlay içindeki TextBlock'a bir x:Name="LoadingMessageText" verebilirsin.
            // LoadingMessageText.Text = message;

            if (isLoading)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // --- Buton Tıklama Olayları ---
        // Artık hepsi tek bir standart yapıyı kullanıyor

        private async void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<DashboardPage>("Dashboard");
        }

        private async void SystemInfo_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<SystemInfoPage>("Sistem Bilgileri");
        }

        private async void ProcessMonitor_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<ProcessMonitorPage>("Süreç Monitörü");
        }

        private async void ActiveAppMonitorMenu_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<ActiveAppMonitorPage>("Aktif Uygulama Takibi");
        }

        private async void UserActivity_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<UserActivityPage>("Kullanıcı Aktiviteleri");
        }

        private async void StartupPrograms_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<StartupProgramsPage>("Başlangıç Programları");
        }

        private async void NetworkMonitor_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<NetworkMonitorPage>("Ağ Monitörü");
        }

        private async void FileSystemMonitor_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<FileSystemMonitorPage>("Dosya Sistemi Monitörü");
        }

        private async void UsbMonitor_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<UsbMonitorPage>("USB Monitörü");
        }

        private async void InstalledSoftware_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<InstalledSoftwarePage>("Yüklü Yazılımlar");
        }

        private async void UserSessionInfo_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<UserSessionInfoPage>("Kullanıcı Oturum Bilgileri");
        }
        private async void SecurityPolicy_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<SecurityPolicyPage>("Güvenlik Politikaları");
        }

        private async void EventLogAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage<EventLogAnalyzerPage>("Olay Günlüğü Analizi");
        }
    }
}