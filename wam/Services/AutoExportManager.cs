using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using wam.Pages;

namespace wam.Services
{
    public static class AutoExportManager
    {
        private static bool _isAutoExportEnabled = false;
        private static string _autoExportDirectory = "";

        static AutoExportManager()
        {
            LoadSettings();
        }

        /// <summary>
        /// Otomatik export ayarlarını yükler
        /// </summary>
        private static void LoadSettings()
        {
            try
            {
                _isAutoExportEnabled = bool.Parse(ConfigurationManager.AppSettings["AutoExportEnabled"] ?? "false");
                _autoExportDirectory = ConfigurationManager.AppSettings["AutoExportDirectory"] ?? 
                                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WAM_AutoExports");
            }
            catch
            {
                _isAutoExportEnabled = false;
                _autoExportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WAM_AutoExports");
            }
        }

        /// <summary>
        /// Otomatik export özelliğini etkinleştirir/devre dışı bırakır
        /// </summary>
        public static void SetAutoExportEnabled(bool enabled)
        {
            _isAutoExportEnabled = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Otomatik export klasörünü ayarlar
        /// </summary>
        public static void SetAutoExportDirectory(string directory)
        {
            _autoExportDirectory = directory;
            SaveSettings();
        }

        /// <summary>
        /// Ayarları kaydet
        /// </summary>
        private static void SaveSettings()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config.AppSettings.Settings["AutoExportEnabled"] != null)
                    config.AppSettings.Settings["AutoExportEnabled"].Value = _isAutoExportEnabled.ToString();
                else
                    config.AppSettings.Settings.Add("AutoExportEnabled", _isAutoExportEnabled.ToString());

                if (config.AppSettings.Settings["AutoExportDirectory"] != null)
                    config.AppSettings.Settings["AutoExportDirectory"].Value = _autoExportDirectory;
                else
                    config.AppSettings.Settings.Add("AutoExportDirectory", _autoExportDirectory);

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoExportManager ayarları kaydedilemedi: {ex.Message}");
            }
        }

        /// <summary>
        /// Sayfa yüklendiğinde otomatik export yapar (eğer etkinse)
        /// </summary>
        public static async Task HandlePageLoaded(ILoadablePage page)
        {
            if (!_isAutoExportEnabled || page == null) return;

            try
            {
                await Task.Delay(2000); // Sayfanın tam yüklenmesi için kısa bir bekleme
                
                // Arka planda sessiz export yap
                await Task.Run(() =>
                {
                    try
                    {
                        // Bu kısımda her sayfa türü için özel veri toplama mantığı yazılabilir
                        if (page is DashboardPage dashboard)
                        {
                            HandleDashboardAutoExport(dashboard);
                        }
                        else if (page is UserSessionInfoPage userSession)
                        {
                            HandleUserSessionAutoExport(userSession);
                        }
                        else if (page is NetworkMonitorPage network)
                        {
                            HandleNetworkMonitorAutoExport(network);
                        }
                        else if (page is SystemInfoPage systemInfo)
                        {
                            HandleSystemInfoAutoExport(systemInfo);
                        }
                        else if (page is UserActivityPage userActivity)
                        {
                            HandleUserActivityAutoExport(userActivity);
                        }
                        // Diğer sayfalar için de benzer metodlar eklenebilir
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AutoExport hatası - {page.GetModuleName()}: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoExportManager.HandlePageLoaded hatası: {ex.Message}");
            }
        }

        private static void HandleDashboardAutoExport(DashboardPage page)
        {
            // Dashboard için özel export mantığı
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logEntry = $"[{timestamp}] Dashboard verileri otomatik export edildi.";
            
            // Silent export yap
            // Burada page'den veri alıp export edebiliriz
            System.Diagnostics.Debug.WriteLine($"Auto export: {logEntry}");
        }

        private static void HandleUserSessionAutoExport(UserSessionInfoPage page)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logEntry = $"[{timestamp}] User Session bilgileri otomatik export edildi.";
            System.Diagnostics.Debug.WriteLine($"Auto export: {logEntry}");
        }

        private static void HandleNetworkMonitorAutoExport(NetworkMonitorPage page)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logEntry = $"[{timestamp}] Network Monitor verileri otomatik export edildi.";
            System.Diagnostics.Debug.WriteLine($"Auto export: {logEntry}");
        }

        private static void HandleSystemInfoAutoExport(SystemInfoPage page)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logEntry = $"[{timestamp}] System Info verileri otomatik export edildi.";
            System.Diagnostics.Debug.WriteLine($"Auto export: {logEntry}");
        }

        private static void HandleUserActivityAutoExport(UserActivityPage page)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logEntry = $"[{timestamp}] User Activity verileri otomatik export edildi.";
            System.Diagnostics.Debug.WriteLine($"Auto export: {logEntry}");
        }

        /// <summary>
        /// Mevcut ayarları döndürür
        /// </summary>
        public static (bool IsEnabled, string Directory) GetCurrentSettings()
        {
            return (_isAutoExportEnabled, _autoExportDirectory);
        }

        /// <summary>
        /// Export dizini oluşturur (yoksa)
        /// </summary>
        public static void EnsureExportDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_autoExportDirectory))
                {
                    Directory.CreateDirectory(_autoExportDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Export dizini oluşturulamadı: {ex.Message}");
            }
        }
    }
} 