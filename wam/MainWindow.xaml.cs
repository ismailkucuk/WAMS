using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using wam.Pages;
using Forms = System.Windows.Forms;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace wam
{
    public partial class MainWindow : Window
    {
        private Button _activeButton;
        private Forms.NotifyIcon _trayIcon;
        private bool _minimizeOnClose = false;
        private bool _forceClose = false;

        public MainWindow()
        {
            InitializeComponent();
            // Açılışta Dashboard'u otomatik olarak yükle
            Loaded += MainWindow_Loaded;
            ContentTitle.Text = "Dashboard";
            InitializeTray();
            LoadWindowSettings();
            // Geliştirici deneyimi: VS altında çalışırken kapatınca gerçekten kapansın
            if (System.Diagnostics.Debugger.IsAttached)
            {
                _minimizeOnClose = false;
            }
            Closing += MainWindow_Closing;
            Application.Current.Exit += (_, __) => { try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { } };
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Uygulama tamamen yüklendikten sonra Dashboard'u göster
            await NavigateToPage<DashboardPage>("Dashboard");
            SetActiveButton(DashboardButton);
        }

        private void SetActiveButton(Button button)
        {
            // Önceki aktif butonu normal hale getir
            if (_activeButton != null)
            {
                _activeButton.IsEnabled = true;
            }

            // Yeni aktif butonu ayarla (IsEnabled = false yaparak aktif stili tetikliyoruz)
            _activeButton = button;
            button.IsEnabled = false;
        }

        // BÜTÜN SAYFA GEÇİŞ MANTIĞINI YÖNETEN MERKEZİ VE SAĞLAM METOT
        public async Task NavigateToPage<T>(string title) where T : UserControl, new()
        {
            // Önceki sayfadaki olay aboneliğini kaldır
            if (PageHost.Content is EventLogAnalyzerPage oldEventPage) { oldEventPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is UserActivityPage oldUserActivityPage) { oldUserActivityPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is UserSessionInfoPage oldUserSessionPage) { oldUserSessionPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is SecurityPolicyPage oldSecurityPage) { oldSecurityPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is DashboardPage oldDashboardPage) { oldDashboardPage.LoadingStateChanged -= OnPageLoadingStateChanged; }

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
                if (page is DashboardPage newDashboardPage) { newDashboardPage.LoadingStateChanged += OnPageLoadingStateChanged; }

                // 4. Sayfayı ÖNCE ekrana yerleştir (UI donmasın)
                PageHost.Content = page;

                // 5. Sol navigasyonu sayfa tipine göre otomatik aktif et
                var btn = GetButtonForPageType(typeof(T));
                if (btn != null) SetActiveButton(btn);

                // 6. Eğer sayfa asenkron yükleme gerektiriyorsa, UI thread'inde bekle
                if (page is ILoadablePage loadablePage)
                {
                    await loadablePage.LoadDataAsync();
                }
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

        private Button GetButtonForPageType(Type pageType)
        {
            if (pageType == typeof(DashboardPage)) return DashboardButton;
            if (pageType == typeof(ActiveAppMonitorPage)) return ActiveAppButton;
            if (pageType == typeof(ProcessMonitorPage)) return ProcessMonitorButton;
            if (pageType == typeof(NetworkMonitorPage)) return NetworkMonitorButton;
            if (pageType == typeof(FileSystemMonitorPage)) return FileSystemButton;
            if (pageType == typeof(UsbMonitorPage)) return UsbMonitorButton;
            if (pageType == typeof(SystemInfoPage)) return SystemInfoButton;
            if (pageType == typeof(StartupProgramsPage)) return StartupProgramsButton;
            if (pageType == typeof(InstalledSoftwarePage)) return InstalledSoftwareButton;
            if (pageType == typeof(EventLogAnalyzerPage)) return EventLogButton;
            if (pageType == typeof(UserActivityPage)) return UserActivityButton;
            if (pageType == typeof(UserSessionInfoPage)) return UserSessionButton;
            if (pageType == typeof(SecurityPolicyPage)) return SecurityPolicyButton;
            return null;
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
            SetActiveButton(DashboardButton);
            await NavigateToPage<DashboardPage>("Dashboard");
        }

        private async void SystemInfo_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SystemInfoButton);
            await NavigateToPage<SystemInfoPage>("Sistem Bilgileri");
        }

        private async void ProcessMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ProcessMonitorButton);
            await NavigateToPage<ProcessMonitorPage>("Süreç Monitörü");
        }

        private async void ActiveAppMonitorMenu_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ActiveAppButton);
            await NavigateToPage<ActiveAppMonitorPage>("Aktif Uygulama Takibi");
        }

        private async void UserActivity_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UserActivityButton);
            await NavigateToPage<UserActivityPage>("Kullanıcı Aktiviteleri");
        }

        private async void StartupPrograms_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(StartupProgramsButton);
            await NavigateToPage<StartupProgramsPage>("Başlangıç Programları");
        }

        private async void NetworkMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(NetworkMonitorButton);
            await NavigateToPage<NetworkMonitorPage>("Ağ Monitörü");
        }

        private async void FileSystemMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(FileSystemButton);
            await NavigateToPage<FileSystemMonitorPage>("Dosya Sistemi Monitörü");
        }

        private async void UsbMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UsbMonitorButton);
            await NavigateToPage<UsbMonitorPage>("USB Monitörü");
        }

        private async void InstalledSoftware_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(InstalledSoftwareButton);
            await NavigateToPage<InstalledSoftwarePage>("Yüklü Yazılımlar");
        }

        private async void UserSessionInfo_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UserSessionButton);
            await NavigateToPage<UserSessionInfoPage>("Kullanıcı Oturum Bilgileri");
        }
        private async void SecurityPolicy_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SecurityPolicyButton);
            await NavigateToPage<SecurityPolicyPage>("Güvenlik Politikaları");
        }

        private async void EventLogAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(EventLogButton);
            await NavigateToPage<EventLogAnalyzerPage>("Olay Günlüğü Analizi");
        }
    }

    // ---------------- Tray & Close Handling ----------------
    partial class MainWindow
    {
        private void InitializeTray()
        {
            try
            {
                _trayIcon = new Forms.NotifyIcon
                {
                    Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/wams.ico")).Stream),
                    Visible = true,
                    Text = "WAM"
                };

                var menu = new Forms.ContextMenuStrip();
                menu.Items.Add("Göster", null, (s, e) => ShowMainWindow());
                menu.Items.Add("Seçimi Unut", null, (s, e) => { _minimizeOnClose = false; SaveWindowSettings(); _trayIcon.ShowBalloonTip(1200, "WAM", "Tercih sıfırlandı.", Forms.ToolTipIcon.Info); });
                menu.Items.Add("Çıkış", null, (s, e) => { _forceClose = true; Close(); });
                _trayIcon.ContextMenuStrip = menu;
                _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
            }
            catch { }
        }

        private void ShowMainWindow()
        {
            try
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                Topmost = true; Topmost = false;
                ShowInTaskbar = true;
            }
            catch { }
        }

        private string GetSettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAM");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }

        private void LoadWindowSettings()
        {
            try
            {
                var path = GetSettingsPath();
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var s = JsonSerializer.Deserialize<WindowSettings>(json);
                _minimizeOnClose = s?.MinimizeOnClose ?? false;
            }
            catch { _minimizeOnClose = false; }
        }

        private void SaveWindowSettings()
        {
            try
            {
                var path = GetSettingsPath();
                var s = new WindowSettings { MinimizeOnClose = _minimizeOnClose };
                File.WriteAllText(path, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // VS ile çalışırken build kilidi yaşamamak için her zaman tamamen kapat
            if (System.Diagnostics.Debugger.IsAttached)
            {
                _forceClose = true;
            }
            if (_forceClose)
            {
                try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { }
                SaveWindowSettings();
                return;
            }

            // Ctrl/Shift ile kapatma → tercihi geçici olarak yok say ve diyalogu göster
            bool overridePrompt = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ||
                                   (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (_minimizeOnClose && !overridePrompt)
            {
                e.Cancel = true;
                ShowInTaskbar = false;
                Hide();
                _trayIcon?.ShowBalloonTip(1500, "WAM", "Uygulama simge durumuna küçültüldü.", Forms.ToolTipIcon.Info);
                return;
            }

            // Profesyonel seçenek penceresi
            var dialog = new wam.Dialogs.CloseConfirmDialog
            {
                Owner = this
            };
            bool? result = dialog.ShowDialog();
            if (result == true && dialog.MinimizeSelected)
            {
                e.Cancel = true;
                _minimizeOnClose = dialog.AlwaysMinimize;
                SaveWindowSettings();
                ShowInTaskbar = false;
                Hide();
                _trayIcon?.ShowBalloonTip(1500, "WAM", "Uygulama simge durumuna küçültüldü.", Forms.ToolTipIcon.Info);
            }
            else if (result == true && dialog.CloseSelected)
            {
                _minimizeOnClose = dialog.AlwaysMinimize; // Kullanıcı kapatsa da tercihi kaydet
                SaveWindowSettings();
                try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { }
            }
            else
            {
                e.Cancel = true; // Dialog kapandıysa iptal
            }
        }

        private class WindowSettings
        {
            public bool MinimizeOnClose { get; set; }
        }
    }
}