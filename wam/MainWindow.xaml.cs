using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using wam.Pages;
using wam.Services;
using wam.Helpers;
using Forms = System.Windows.Forms;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using AutoUpdaterDotNET;

namespace wam
{
    public partial class MainWindow : Window
    {
        private Button _activeButton;
        private Forms.NotifyIcon _trayIcon;
        private bool _minimizeOnClose = false;
        private bool _forceClose = false;
        private bool _rememberChoice = false;
        private SnowOverlayWindow? _snowOverlay;

        private const string UpdateUrl = "https://raw.githubusercontent.com/ismailkucuk/WAMS/master/update.xml";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            ContentTitle.Text = LocalizationService.Instance.GetString("PageTitle_Dashboard", "Dashboard");
            InitializeTray();
            LoadWindowSettings();

            ConfigureAutoUpdater();

            if (System.Diagnostics.Debugger.IsAttached)
            {
                _minimizeOnClose = false;
            }
            Closing += MainWindow_Closing;
            Application.Current.Exit += (_, __) => {
                try { DisposeSnowOverlay(); } catch { }
                try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { }
            };
        }

        /// <summary>
        /// Configures and starts AutoUpdater to check for updates on startup.
        /// </summary>
        private void ConfigureAutoUpdater()
        {
            try
            {
                // AutoUpdater settings
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.RunUpdateAsAdmin = true;
                AutoUpdater.Synchronous = false;
                AutoUpdater.ReportErrors = false; // Don't show error dialogs on startup check

                // Start update check
                AutoUpdater.Start(UpdateUrl);

                System.Diagnostics.Debug.WriteLine("AutoUpdater: Startup check initiated.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoUpdater configuration error: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually checks for updates and shows a message if no update is available.
        /// Call this from a "Check for Updates" button.
        /// </summary>
        public void CheckForUpdatesManually()
        {
            try
            {
                AutoUpdater.ReportErrors = true; // Show errors during manual check
                AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
                AutoUpdater.Start(UpdateUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güncelleme kontrolü sırasında hata oluştu:\n{ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            try
            {
                // Unsubscribe to prevent multiple calls
                AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;

                if (args.Error == null)
                {
                    if (!args.IsUpdateAvailable)
                    {
                        var message = LocalizationService.Instance.CurrentLanguage == "tr-TR"
                            ? "En güncel sürümü kullanıyorsunuz."
                            : "You are using the latest version.";
                        var title = LocalizationService.Instance.CurrentLanguage == "tr-TR"
                            ? "Güncelleme Yok"
                            : "No Update Available";

                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    // If update is available, AutoUpdater will show its built-in dialog
                }
                else
                {
                    var message = LocalizationService.Instance.CurrentLanguage == "tr-TR"
                        ? $"Güncelleme kontrol edilemedi:\n{args.Error.Message}"
                        : $"Could not check for updates:\n{args.Error.Message}";

                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoUpdater event handler error: {ex.Message}");
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize snow effect overlay based on config
            InitializeSnowOverlay();

            // Uygulama tamamen yüklendikten sonra Dashboard'u göster
            await NavigateToPage<DashboardPage>();
            SetActiveButton(DashboardButton);
        }

        /// <summary>
        /// Initializes the transparent snow overlay window that covers the entire application.
        /// The overlay is click-through, allowing user interaction with controls below.
        /// Also switches the logo to winter version when snow effect is enabled.
        /// </summary>
        private void InitializeSnowOverlay()
        {
            try
            {
                var config = ConfigService.LoadConfig();

                // Feature 1: Dynamic Logo Switching
                UpdateLogoForSeason(config.SnowEffect);

                if (!config.SnowEffect)
                {
                    System.Diagnostics.Debug.WriteLine("Snow effect is disabled in config.");
                    return;
                }

                // Create and configure the overlay window
                _snowOverlay = new SnowOverlayWindow();
                _snowOverlay.Initialize(config);
                _snowOverlay.BindToOwner(this);
                _snowOverlay.Show();
                _snowOverlay.Start();

                System.Diagnostics.Debug.WriteLine("Snow overlay initialized and started successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize snow overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Switches the application logo based on snow effect configuration.
        /// Uses winter logo when snow effect is enabled, normal logo otherwise.
        /// </summary>
        private void UpdateLogoForSeason(bool isWinterMode)
        {
            try
            {
                string logoPath = isWinterMode
                    ? "pack://application:,,,/Resources/logo_winter.png"
                    : "pack://application:,,,/Resources/logo_normal.png";

                var bitmap = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
                AppLogo.Source = bitmap;

                System.Diagnostics.Debug.WriteLine($"Logo switched to: {(isWinterMode ? "Winter" : "Normal")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to switch logo: {ex.Message}");
                // Keep the default logo on failure
            }
        }

        /// <summary>
        /// Disposes the snow overlay window.
        /// </summary>
        private void DisposeSnowOverlay()
        {
            if (_snowOverlay != null)
            {
                try
                {
                    _snowOverlay.Stop();
                    _snowOverlay.Close();
                    _snowOverlay = null;
                    System.Diagnostics.Debug.WriteLine("Snow overlay disposed.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing snow overlay: {ex.Message}");
                }
            }
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
        public async Task NavigateToPage<T>(string title = null) where T : UserControl, new()
        {
            // Önceki sayfadaki olay aboneliğini kaldır
            if (PageHost.Content is EventLogAnalyzerPage oldEventPage) { oldEventPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is UserActivityPage oldUserActivityPage) { oldUserActivityPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is UserSessionInfoPage oldUserSessionPage) { oldUserSessionPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is SecurityPolicyPage oldSecurityPage) { oldSecurityPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is DashboardPage oldDashboardPage) { oldDashboardPage.LoadingStateChanged -= OnPageLoadingStateChanged; }
            if (PageHost.Content is NetworkMonitorPage oldNetworkPage) { oldNetworkPage.LoadingStateChanged -= OnPageLoadingStateChanged; }

            // 1. Yükleme ekranını göster
            LoadingOverlay.Visibility = Visibility.Visible;
            // Lokalize edilmiş başlık al
            var titleKey = GetPageTitleKey(typeof(T));
            ContentTitle.Text = titleKey != null
                ? LocalizationService.Instance.GetString(titleKey, title ?? "Dashboard")
                : title ?? "Dashboard";
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
                if (page is NetworkMonitorPage newNetworkPage) { newNetworkPage.LoadingStateChanged += OnPageLoadingStateChanged; }

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
            if (pageType == typeof(SettingsPage)) return SettingsButton;
            return null;
        }

        private string GetPageTitleKey(Type pageType)
        {
            if (pageType == typeof(DashboardPage)) return "PageTitle_Dashboard";
            if (pageType == typeof(ActiveAppMonitorPage)) return "PageTitle_ActiveApps";
            if (pageType == typeof(ProcessMonitorPage)) return "PageTitle_ProcessMonitor";
            if (pageType == typeof(NetworkMonitorPage)) return "PageTitle_NetworkMonitor";
            if (pageType == typeof(FileSystemMonitorPage)) return "PageTitle_FileSystem";
            if (pageType == typeof(UsbMonitorPage)) return "PageTitle_UsbMonitor";
            if (pageType == typeof(SystemInfoPage)) return "PageTitle_SystemInfo";
            if (pageType == typeof(StartupProgramsPage)) return "PageTitle_StartupPrograms";
            if (pageType == typeof(InstalledSoftwarePage)) return "PageTitle_InstalledSoftware";
            if (pageType == typeof(EventLogAnalyzerPage)) return "PageTitle_EventLog";
            if (pageType == typeof(UserActivityPage)) return "PageTitle_UserActivity";
            if (pageType == typeof(UserSessionInfoPage)) return "PageTitle_UserSession";
            if (pageType == typeof(SecurityPolicyPage)) return "PageTitle_SecurityPolicy";
            if (pageType == typeof(SettingsPage)) return "PageTitle_Settings";
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
            await NavigateToPage<DashboardPage>();
        }

        private async void SystemInfo_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SystemInfoButton);
            await NavigateToPage<SystemInfoPage>();
        }

        private async void ProcessMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ProcessMonitorButton);
            await NavigateToPage<ProcessMonitorPage>();
        }

        private async void ActiveAppMonitorMenu_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ActiveAppButton);
            await NavigateToPage<ActiveAppMonitorPage>();
        }

        private async void UserActivity_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UserActivityButton);
            await NavigateToPage<UserActivityPage>();
        }

        private async void StartupPrograms_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(StartupProgramsButton);
            await NavigateToPage<StartupProgramsPage>();
        }

        private async void NetworkMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(NetworkMonitorButton);
            await NavigateToPage<NetworkMonitorPage>();
        }

        private async void FileSystemMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(FileSystemButton);
            await NavigateToPage<FileSystemMonitorPage>();
        }

        private async void UsbMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UsbMonitorButton);
            await NavigateToPage<UsbMonitorPage>();
        }

        private async void InstalledSoftware_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(InstalledSoftwareButton);
            await NavigateToPage<InstalledSoftwarePage>();
        }

        private async void UserSessionInfo_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UserSessionButton);
            await NavigateToPage<UserSessionInfoPage>();
        }

        private async void SecurityPolicy_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SecurityPolicyButton);
            await NavigateToPage<SecurityPolicyPage>();
        }

        private async void EventLogAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(EventLogButton);
            await NavigateToPage<EventLogAnalyzerPage>();
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SettingsButton);
            await NavigateToPage<SettingsPage>();
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
                menu.Items.Add("Seçimi Unut", null, (s, e) => { _rememberChoice = false; SaveWindowSettings(); _trayIcon.ShowBalloonTip(1200, "WAM", "Tercih sıfırlandı.", Forms.ToolTipIcon.Info); });
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
                _rememberChoice = s?.RememberChoice ?? false;
            }
            catch { _minimizeOnClose = false; }
        }

        private void SaveWindowSettings()
        {
            try
            {
                var path = GetSettingsPath();
                var s = new WindowSettings { MinimizeOnClose = _minimizeOnClose, RememberChoice = _rememberChoice };
                File.WriteAllText(path, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        // SettingsPage'den anında uygulamak için
        public void ApplyMinimizeOnCloseSetting(bool minimizeOnClose)
        {
            _minimizeOnClose = minimizeOnClose;
            _rememberChoice = true; // Ayarlardan gelen tercih her zaman hatırlansın
            SaveWindowSettings();
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
                try { DisposeSnowOverlay(); } catch { }
                try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { }
                SaveWindowSettings();
                return;
            }

            // Ctrl/Shift ile kapatma → tercihi geçici olarak yok say ve diyalogu göster
            bool overridePrompt = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ||
                                   (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            // Kaydedilmiş tercih varsa diyalog göstermeden uygula
            if (_rememberChoice && !overridePrompt)
            {
                if (_minimizeOnClose)
                {
                    e.Cancel = true;
                    ShowInTaskbar = false;
                    Hide();
                    _trayIcon?.ShowBalloonTip(1500, "WAM", "Uygulama simge durumuna küçültüldü.", Forms.ToolTipIcon.Info);
                    return;
                }
                else
                {
                    // Kapatmayı tercih ediyorsa doğrudan kapat
                    SaveWindowSettings();
                    return; // e.Cancel = false → kapanmaya devam
                }
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
                if (dialog.AlwaysMinimize)
                {
                    _rememberChoice = true;
                    _minimizeOnClose = true;
                }
                SaveWindowSettings();
                ShowInTaskbar = false;
                Hide();
                _trayIcon?.ShowBalloonTip(1500, "WAM", "Uygulama simge durumuna küçültüldü.", Forms.ToolTipIcon.Info);
            }
            else if (result == true && dialog.CloseSelected)
            {
                if (dialog.AlwaysMinimize)
                {
                    _rememberChoice = true;
                    _minimizeOnClose = false;
                }
                SaveWindowSettings();
                try { DisposeSnowOverlay(); } catch { }
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
            public bool RememberChoice { get; set; }
        }
    }
}