using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using wam.Services;

namespace wam.Pages
{
    public partial class SecurityPolicyPage : UserControl, ILoadablePage
    {
        private readonly SecurityPolicyService _securityService;
        private SecurityPolicies _currentPolicies;

        public SecurityPolicyPage()
        {
            InitializeComponent();
            _securityService = new SecurityPolicyService();
        }

        public async Task LoadDataAsync()
        {
            LoadingStateChanged?.Invoke(true, "Güvenlik politikaları yükleniyor...");
            try
            {
                await LoadSecurityPoliciesAsync();
            }
            finally
            {
                LoadingStateChanged?.Invoke(false, "");
            }
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            if (_currentPolicies == null)
            {
                MessageBox.Show("Önce güvenlik politikalarını yükleyin.", "Veri Yok", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var exportData = new
            {
                SecurityPolicies = new
                {
                    IsPasswordRequired = _currentPolicies.IsPasswordRequired,
                    MinPasswordLength = _currentPolicies.MinPasswordLength,
                    IsPasswordComplexityEnabled = _currentPolicies.IsPasswordComplexityEnabled,
                    IsUacEnabled = _currentPolicies.IsUacEnabled,
                    IsRdpEnabled = _currentPolicies.IsRdpEnabled,
                    IsGuestEnabled = _currentPolicies.IsGuestEnabled,
                    IsAdministratorEnabled = _currentPolicies.IsAdministratorEnabled,
                    SecurityScore = _currentPolicies.SecurityScore
                },
                Recommendations = _securityService.GetRecommendations(_currentPolicies)
            };

            ExportService.ExportToJson(new[] { exportData }, GetModuleName());
        }

        public void ExportToCsv()
        {
            if (_currentPolicies == null)
            {
                MessageBox.Show("Önce güvenlik politikalarını yükleyin.", "Veri Yok", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var csvData = new List<dynamic>
            {
                new
                {
                    PolicyType = "Password",
                    PolicyName = "Password Required",
                    Status = _currentPolicies.IsPasswordRequired ? "Enabled" : "Disabled",
                    Recommendation = _currentPolicies.IsPasswordRequired ? "Good" : "Enable password requirement"
                },
                new
                {
                    PolicyType = "Password",
                    PolicyName = "Minimum Password Length",
                    Status = _currentPolicies.MinPasswordLength.ToString(),
                    Recommendation = _currentPolicies.MinPasswordLength >= 8 ? "Good" : "Increase to at least 8 characters"
                },
                new
                {
                    PolicyType = "Password",
                    PolicyName = "Password Complexity",
                    Status = _currentPolicies.IsPasswordComplexityEnabled ? "Enabled" : "Disabled",
                    Recommendation = _currentPolicies.IsPasswordComplexityEnabled ? "Good" : "Enable password complexity"
                },
                new
                {
                    PolicyType = "System",
                    PolicyName = "UAC",
                    Status = _currentPolicies.IsUacEnabled ? "Enabled" : "Disabled",
                    Recommendation = _currentPolicies.IsUacEnabled ? "Good" : "Enable UAC for better security"
                },
                new
                {
                    PolicyType = "System",
                    PolicyName = "RDP",
                    Status = _currentPolicies.IsRdpEnabled ? "Enabled" : "Disabled",
                    Recommendation = !_currentPolicies.IsRdpEnabled ? "Good" : "Consider disabling RDP if not needed"
                },
                new
                {
                    PolicyType = "Account",
                    PolicyName = "Guest Account",
                    Status = _currentPolicies.IsGuestEnabled ? "Enabled" : "Disabled",
                    Recommendation = !_currentPolicies.IsGuestEnabled ? "Good" : "Disable guest account"
                },
                new
                {
                    PolicyType = "Account",
                    PolicyName = "Administrator Account",
                    Status = _currentPolicies.IsAdministratorEnabled ? "Enabled" : "Disabled",
                    Recommendation = !_currentPolicies.IsAdministratorEnabled ? "Good" : "Consider disabling built-in admin"
                },
                new
                {
                    PolicyType = "Overall",
                    PolicyName = "Security Score",
                    Status = _currentPolicies.SecurityScore.ToString(),
                    Recommendation = GetScoreRecommendation(_currentPolicies.SecurityScore)
                }
            };

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            if (_currentPolicies == null)
            {
                MessageBox.Show("Önce güvenlik politikalarını yükleyin.", "Veri Yok", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var exportData = new
            {
                SecurityPolicies = new
                {
                    IsPasswordRequired = _currentPolicies.IsPasswordRequired,
                    MinPasswordLength = _currentPolicies.MinPasswordLength,
                    IsPasswordComplexityEnabled = _currentPolicies.IsPasswordComplexityEnabled,
                    IsUacEnabled = _currentPolicies.IsUacEnabled,
                    IsRdpEnabled = _currentPolicies.IsRdpEnabled,
                    IsGuestEnabled = _currentPolicies.IsGuestEnabled,
                    IsAdministratorEnabled = _currentPolicies.IsAdministratorEnabled,
                    SecurityScore = _currentPolicies.SecurityScore
                },
                Recommendations = _securityService.GetRecommendations(_currentPolicies)
            };

            ExportService.AutoExport(new[] { exportData }, GetModuleName());
        }

        public string GetModuleName()
        {
            return "SecurityPolicy";
        }

        private string GetScoreRecommendation(int score)
        {
            if (score >= 80) return "Excellent security posture";
            if (score >= 60) return "Good security, minor improvements needed";
            if (score >= 40) return "Moderate security, several improvements needed";
            return "Poor security, immediate action required";
        }

        private async Task LoadSecurityPoliciesAsync()
        {
            try
            {
                _currentPolicies = await Task.Run(() => _securityService.GetSecurityPolicies());
                
                // UI'yi güncelle
                UpdatePasswordPolicies(_currentPolicies);
                UpdateSystemSecurity(_currentPolicies);
                UpdateAccountStatuses(_currentPolicies);
                UpdateSecurityScore(_currentPolicies);
            }
            catch (Exception ex)
            {
                // Hata durumunda güvenlik skoru kırmızı yap
                SecurityScoreValue.Text = "ERR";
                SecurityScoreValue.Foreground = new SolidColorBrush(Colors.Red);
                SecurityScoreLabel.Text = $"Hata: {ex.Message}";
            }
        }

        private void UpdatePasswordPolicies(SecurityPolicies policies)
        {
            // Parola gerekli
            PasswordRequiredStatus.Text = policies.IsPasswordRequired ? "Evet" : "Hayır";
            PasswordRequiredStatus.Foreground = GetStatusBrush(policies.IsPasswordRequired);
            PasswordRequiredBorder.BorderBrush = GetStatusBrush(policies.IsPasswordRequired);

            // Minimum parola uzunluğu
            MinPasswordLengthValue.Text = policies.MinPasswordLength.ToString();
            bool lengthOk = policies.MinPasswordLength >= 8;
            MinPasswordLengthValue.Foreground = GetStatusBrush(lengthOk);
            MinPasswordLengthBorder.BorderBrush = GetStatusBrush(lengthOk);

            // Karmaşık parola
            ComplexPasswordStatus.Text = policies.IsPasswordComplexityEnabled ? "Açık" : "Kapalı";
            ComplexPasswordStatus.Foreground = GetStatusBrush(policies.IsPasswordComplexityEnabled);
            ComplexPasswordBorder.BorderBrush = GetStatusBrush(policies.IsPasswordComplexityEnabled);
        }

        private void UpdateSystemSecurity(SecurityPolicies policies)
        {
            // UAC
            UacStatus.Text = policies.IsUacEnabled ? "Etkin" : "Devre Dışı";
            UacStatus.Foreground = GetStatusBrush(policies.IsUacEnabled);
            UacBorder.BorderBrush = GetStatusBrush(policies.IsUacEnabled);

            // RDP
            RdpStatus.Text = policies.IsRdpEnabled ? "Açık" : "Kapalı";
            RdpStatus.Foreground = GetStatusBrush(!policies.IsRdpEnabled); // RDP kapalı olması daha güvenli
            RdpBorder.BorderBrush = GetStatusBrush(!policies.IsRdpEnabled);
        }

        private void UpdateAccountStatuses(SecurityPolicies policies)
        {
            // Guest hesabı
            GuestStatus.Text = policies.IsGuestEnabled ? "Etkin" : "Devre Dışı";
            GuestStatus.Foreground = GetStatusBrush(!policies.IsGuestEnabled); // Guest kapalı olması daha güvenli
            GuestBorder.BorderBrush = GetStatusBrush(!policies.IsGuestEnabled);

            // Administrator hesabı
            AdministratorStatus.Text = policies.IsAdministratorEnabled ? "Etkin" : "Devre Dışı";
            AdministratorStatus.Foreground = GetStatusBrush(!policies.IsAdministratorEnabled); // Admin kapalı olması daha güvenli
            AdministratorBorder.BorderBrush = GetStatusBrush(!policies.IsAdministratorEnabled);
        }

        private void UpdateSecurityScore(SecurityPolicies policies)
        {
            SecurityScoreValue.Text = policies.SecurityScore.ToString();
            SecurityScoreValue.Foreground = GetScoreBrush(policies.SecurityScore);

            // Skor rengine göre label'ı güncelle
            if (policies.SecurityScore >= 80)
                SecurityScoreLabel.Text = "Mükemmel";
            else if (policies.SecurityScore >= 60)
                SecurityScoreLabel.Text = "İyi";
            else if (policies.SecurityScore >= 40)
                SecurityScoreLabel.Text = "Orta";
            else
                SecurityScoreLabel.Text = "Zayıf";

            // Önerileri ekle
            AddRecommendations(policies);
        }

        private void AddRecommendations(SecurityPolicies policies)
        {
            RecommendationsPanel.Children.Clear();

            var recommendations = _securityService.GetRecommendations(policies);
            foreach (var recommendation in recommendations)
            {
                var textBlock = new TextBlock
                {
                    Text = "• " + recommendation,
                    FontSize = 12,
                    Foreground = (SolidColorBrush)FindResource("TextColorSecondary"),
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Margin = new System.Windows.Thickness(0, 0, 0, 8)
                };
                RecommendationsPanel.Children.Add(textBlock);
            }
        }

        private SolidColorBrush GetStatusBrush(bool isGood)
        {
            return isGood ? 
                (SolidColorBrush)FindResource("SuccessColor") : 
                (SolidColorBrush)FindResource("DangerColor");
        }

        private SolidColorBrush GetScoreBrush(int score)
        {
            if (score >= 80) return (SolidColorBrush)FindResource("SuccessColor");
            if (score >= 60) return (SolidColorBrush)FindResource("AccentColor");
            if (score >= 40) return (SolidColorBrush)FindResource("WarningColor");
            return (SolidColorBrush)FindResource("DangerColor");
        }

        public event Action<bool, string> LoadingStateChanged;

        // Düzenleme butonları için olay işleyiciler
        private void EditPasswordRequired_Click(object sender, RoutedEventArgs e)
        {
            OpenSecurityPolicySettings("Parola politikalarını düzenlemek için Yerel Güvenlik İlkesi'ni açın.", 
                "secpol.msc", "/s \"Hesap İlkeleri\\Parola İlkesi\"");
        }

        private void EditMinPasswordLength_Click(object sender, RoutedEventArgs e)
        {
            OpenSecurityPolicySettings("Minimum parola uzunluğunu değiştirmek için Yerel Güvenlik İlkesi'ni açın.", 
                "secpol.msc", "/s \"Hesap İlkeleri\\Parola İlkesi\"");
        }

        private void EditComplexPassword_Click(object sender, RoutedEventArgs e)
        {
            OpenSecurityPolicySettings("Karmaşık parola gereksinimini düzenlemek için Yerel Güvenlik İlkesi'ni açın.", 
                "secpol.msc", "/s \"Hesap İlkeleri\\Parola İlkesi\"");
        }

        private void EditUac_Click(object sender, RoutedEventArgs e)
        {
            OpenSystemSettings("UAC ayarlarını değiştirmek için Kullanıcı Hesabı Denetimi ayarlarını açın.", 
                "UserAccountControlSettings.exe", "");
        }

        private void EditRdp_Click(object sender, RoutedEventArgs e)
        {
            OpenSystemSettings("Uzak Masaüstü ayarlarını değiştirmek için Sistem Özellikleri'ni açın.", 
                "SystemPropertiesRemote.exe", "");
        }

        private void EditGuestAccount_Click(object sender, RoutedEventArgs e)
        {
            OpenSystemSettings("Guest hesabını yönetmek için Bilgisayar Yönetimi'ni açın.", 
                "compmgmt.msc", "/s");
        }

        private void EditAdministratorAccount_Click(object sender, RoutedEventArgs e)
        {
            OpenSystemSettings("Administrator hesabını yönetmek için Bilgisayar Yönetimi'ni açın.", 
                "compmgmt.msc", "/s");
        }

        private void OpenSecurityPolicySettings(string message, string executable, string arguments)
        {
            try
            {
                var result = MessageBox.Show(
                    $"{message}\n\nNot: Bu işlem yönetici izinleri gerektirir.\n\nDevam etmek istiyor musunuz?",
                    "Güvenlik Ayarları",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Önce tam sistem yolunu kontrol et
                    string fullPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), executable);
                    
                    if (!System.IO.File.Exists(fullPath))
                    {
                        // secpol.msc bulunamazsa alternatif yöntem: GPEdit veya Control Panel
                        OpenAlternativeSecuritySettings();
                        return;
                    }

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        Arguments = arguments,
                        UseShellExecute = true,
                        Verb = "runas", // Yönetici olarak çalıştır
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System)
                    };

                    System.Diagnostics.Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ayarlar sayfası açılırken bir hata oluştu:\n\n{ex.Message}\n\nAlternatif yöntem deneniyor...",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                // Hata durumunda alternatif yöntem dene
                OpenAlternativeSecuritySettings();
            }
        }

        private void OpenAlternativeSecuritySettings()
        {
            try
            {
                // Alternatif 1: Group Policy Editor (gpedit.msc)
                string gpeditPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "gpedit.msc");
                
                if (System.IO.File.Exists(gpeditPath))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = gpeditPath,
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System)
                    };
                    
                    System.Diagnostics.Process.Start(startInfo);
                    
                    MessageBox.Show(
                        "Grup İlkesi Düzenleyicisi açıldı.\n\nParola politikalarını düzenlemek için:\n" +
                        "Bilgisayar Yapılandırması → Windows Ayarları → Güvenlik Ayarları → Hesap İlkeleri → Parola İlkesi",
                        "Yönlendirme",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
                
                // Alternatif 2: Netplwiz (Kullanıcı Hesapları)
                var netplwizStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netplwiz.exe",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                
                System.Diagnostics.Process.Start(netplwizStartInfo);
                
                MessageBox.Show(
                    "Kullanıcı Hesapları açıldı.\n\nGelişmiş ayarlar için 'Gelişmiş' sekmesini kullanın.",
                    "Alternatif Yöntem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Alternatif yöntemler de başarısız oldu:\n\n{ex.Message}\n\n" +
                    "Lütfen manuel olarak şu adımları izleyin:\n" +
                    "1. Başlat menüsünden 'Yerel Güvenlik İlkesi' arayın\n" +
                    "2. Veya 'Grup İlkesi Düzenleyicisi' açın\n" +
                    "3. Hesap İlkeleri → Parola İlkesi bölümüne gidin",
                    "Manuel İşlem Gerekli",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void OpenSystemSettings(string message, string executable, string arguments)
        {
            try
            {
                var result = MessageBox.Show(
                    $"{message}\n\nNot: Bu işlem yönetici izinleri gerektirebilir.\n\nDevam etmek istiyor musunuz?",
                    "Sistem Ayarları",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Sistem dosyalarının tam yolunu belirle
                    string fullPath = GetFullExecutablePath(executable);
                    
                    if (string.IsNullOrEmpty(fullPath))
                    {
                        OpenAlternativeSystemSettings(executable);
                        return;
                    }

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        Arguments = arguments,
                        UseShellExecute = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System)
                    };

                    // Bazı araçlar için yönetici izni gerekli
                    if (executable.Contains("compmgmt") || executable.Contains("msc"))
                    {
                        startInfo.Verb = "runas";
                    }

                    System.Diagnostics.Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ayarlar sayfası açılırken bir hata oluştu:\n\n{ex.Message}\n\nAlternatif yöntem deneniyor...",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                OpenAlternativeSystemSettings(executable);
            }
        }

        private string GetFullExecutablePath(string executable)
        {
            // Önce System32 klasöründe ara
            string systemPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), executable);
            if (System.IO.File.Exists(systemPath))
                return systemPath;

            // Windows klasöründe ara
            string windowsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), executable);
            if (System.IO.File.Exists(windowsPath))
                return windowsPath;

            // System32 alt klasörlerinde ara
            string[] subFolders = { "wbem", "drivers", "config" };
            foreach (string folder in subFolders)
            {
                string subPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), folder, executable);
                if (System.IO.File.Exists(subPath))
                    return subPath;
            }

            return null;
        }

        private void OpenAlternativeSystemSettings(string originalExecutable)
        {
            try
            {
                if (originalExecutable.Contains("UserAccountControl"))
                {
                    // UAC için alternatif: Control Panel
                    System.Diagnostics.Process.Start("control.exe", "userpasswords");
                    MessageBox.Show(
                        "Kullanıcı Hesapları Denetim Paneli açıldı.\n\nUAC ayarları için 'Kullanıcı Hesabı Denetimi ayarlarını değiştir' seçeneğini arayın.",
                        "Alternatif Yöntem",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (originalExecutable.Contains("SystemProperties"))
                {
                    // RDP için alternatif: System Properties
                    System.Diagnostics.Process.Start("control.exe", "sysdm.cpl,,5");
                    MessageBox.Show(
                        "Sistem Özellikleri açıldı.\n\n'Uzak' sekmesinde Uzak Masaüstü ayarlarını bulabilirsiniz.",
                        "Alternatif Yöntem",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (originalExecutable.Contains("compmgmt"))
                {
                    // Computer Management için alternatif: User Management via Control Panel
                    System.Diagnostics.Process.Start("control.exe", "userpasswords2");
                    MessageBox.Show(
                        "Gelişmiş Kullanıcı Hesapları açıldı.\n\nKullanıcı hesaplarını yönetmek için 'Gelişmiş' sekmesini kullanın.",
                        "Alternatif Yöntem",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Genel alternatif: Settings app
                    System.Diagnostics.Process.Start("ms-settings:privacy-accounts");
                    MessageBox.Show(
                        "Windows Ayarları açıldı.\n\nHesaplar ve güvenlik ayarlarını buradan yönetebilirsiniz.",
                        "Alternatif Yöntem",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Alternatif yöntemler de başarısız oldu:\n\n{ex.Message}\n\n" +
                    "Lütfen manuel olarak Windows Ayarları'nı açın ve ilgili bölümü arayın.",
                    "Manuel İşlem Gerekli",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    public class SecurityPolicies
    {
        public bool IsPasswordRequired { get; set; }
        public int MinPasswordLength { get; set; }
        public bool IsPasswordComplexityEnabled { get; set; }
        public bool IsUacEnabled { get; set; }
        public bool IsRdpEnabled { get; set; }
        public bool IsGuestEnabled { get; set; }
        public bool IsAdministratorEnabled { get; set; }
        public int SecurityScore { get; set; }
    }

    public class SecurityPolicyService
    {
        public SecurityPolicies GetSecurityPolicies()
        {
            var policies = new SecurityPolicies();

            try
            {
                policies.IsPasswordRequired = IsPasswordRequired();
                policies.MinPasswordLength = GetMinimumPasswordLength();
                policies.IsPasswordComplexityEnabled = IsPasswordComplexityEnabled();
                policies.IsUacEnabled = IsUacEnabled();
                policies.IsRdpEnabled = IsRdpEnabled();
                policies.IsGuestEnabled = IsAccountEnabled("Guest");
                policies.IsAdministratorEnabled = IsAccountEnabled("Administrator");
                policies.SecurityScore = CalculateSecurityScore(policies);
            }
            catch
            {
                // Hata durumunda varsayılan değerler
                policies.SecurityScore = 0;
            }

            return policies;
        }

        public List<string> GetRecommendations(SecurityPolicies policies)
        {
            var recommendations = new List<string>();

            if (!policies.IsPasswordRequired)
                recommendations.Add("Tüm kullanıcılar için parola zorunluluğu etkinleştirin");

            if (policies.MinPasswordLength < 8)
                recommendations.Add("Minimum parola uzunluğunu en az 8 karakter yapın");

            if (!policies.IsPasswordComplexityEnabled)
                recommendations.Add("Karmaşık parola gereksinimini etkinleştirin");

            if (!policies.IsUacEnabled)
                recommendations.Add("UAC (Kullanıcı Hesabı Denetimi) özelliğini etkinleştirin");

            if (policies.IsRdpEnabled)
                recommendations.Add("Gerekli değilse RDP (Uzak Masaüstü) özelliğini devre dışı bırakın");

            if (policies.IsGuestEnabled)
                recommendations.Add("Guest (Konuk) hesabını devre dışı bırakın");

            if (policies.IsAdministratorEnabled)
                recommendations.Add("Yerleşik Administrator hesabını devre dışı bırakın");

            if (recommendations.Count == 0)
                recommendations.Add("Güvenlik yapılandırmanız mükemmel görünüyor!");

            return recommendations;
        }

        private int CalculateSecurityScore(SecurityPolicies policies)
        {
            int score = 0;

            if (policies.IsPasswordRequired) score += 20;
            if (policies.MinPasswordLength >= 8) score += 15;
            if (policies.IsPasswordComplexityEnabled) score += 15;
            if (policies.IsUacEnabled) score += 15;
            if (!policies.IsRdpEnabled) score += 15;
            if (!policies.IsGuestEnabled) score += 10;
            if (!policies.IsAdministratorEnabled) score += 10;

            return score;
        }

        private bool IsPasswordRequired()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa");
                return key?.GetValue("PasswordHistorySize") != null;
            }
            catch { return false; }
        }

        private int GetMinimumPasswordLength()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa");
                var value = key?.GetValue("MinimumPasswordLength");
                return value != null ? Convert.ToInt32(value) : 0;
            }
            catch { return 0; }
        }

        private bool IsPasswordComplexityEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa");
                var value = key?.GetValue("PasswordComplexity");
                return value != null && Convert.ToInt32(value) == 1;
            }
            catch { return false; }
        }

        private bool IsUacEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                return key?.GetValue("EnableLUA")?.ToString() == "1";
            }
            catch { return false; }
        }

        private bool IsRdpEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server");
                return key?.GetValue("fDenyTSConnections")?.ToString() == "0";
            }
            catch { return false; }
        }

        private bool IsAccountEnabled(string userName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT Disabled FROM Win32_UserAccount WHERE Name = '{userName}' AND LocalAccount = TRUE");

                foreach (ManagementObject obj in searcher.Get())
                {
                    return !(bool)obj["Disabled"];
                }
            }
            catch { }
            return false;
        }
    }
}
