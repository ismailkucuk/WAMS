using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using wam.Services;

namespace wam.Pages
{
    public class ScoreToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int score = System.Convert.ToInt32(value);
                Color color = score >= 80 ? Color.FromRgb(40, 167, 69) : // Success
                              score >= 60 ? Color.FromRgb(74, 144, 226) : // Accent
                              score >= 40 ? Color.FromRgb(255, 193, 7) :  // Warning
                                            Color.FromRgb(220, 53, 69);   // Danger
                return new SolidColorBrush(color);
            }
            catch { return new SolidColorBrush(Colors.Gray); }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class SecurityPolicyPage : UserControl, ILoadablePage, System.ComponentModel.INotifyPropertyChanged
    {
        private readonly SecurityPolicyService _securityService;
        private SecurityPolicies _currentPolicies;

        public ObservableCollection<PolicyItem> PasswordPolicies { get; } = new ObservableCollection<PolicyItem>();
        public ObservableCollection<PolicyItem> SystemPolicies { get; } = new ObservableCollection<PolicyItem>();
        public ObservableCollection<PolicyItem> AccountPolicies { get; } = new ObservableCollection<PolicyItem>();
        public ObservableCollection<string> Recommendations { get; } = new ObservableCollection<string>();
        public ObservableCollection<IssueItem> CriticalIssues { get; } = new ObservableCollection<IssueItem>();
        public ObservableCollection<IssueItem> WarningIssues { get; } = new ObservableCollection<IssueItem>();
        public ObservableCollection<string> PassedChecks { get; } = new ObservableCollection<string>();
        public int SecurityScore { get; set; }
        public string SecurityScoreLabel { get; set; }

        public SecurityPolicyPage()
        {
            InitializeComponent();
            _securityService = new SecurityPolicyService();
            DataContext = this;
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
                new { PolicyType = "Password", PolicyName = "Password Required", Status = _currentPolicies.IsPasswordRequired ? "Enabled" : "Disabled", Recommendation = _currentPolicies.IsPasswordRequired ? "Good" : "Enable password requirement" },
                new { PolicyType = "Password", PolicyName = "Minimum Password Length", Status = _currentPolicies.MinPasswordLength.ToString(), Recommendation = _currentPolicies.MinPasswordLength >= 8 ? "Good" : "Increase to at least 8 characters" },
                new { PolicyType = "Password", PolicyName = "Password Complexity", Status = _currentPolicies.IsPasswordComplexityEnabled ? "Enabled" : "Disabled", Recommendation = _currentPolicies.IsPasswordComplexityEnabled ? "Good" : "Enable password complexity" },
                new { PolicyType = "System", PolicyName = "UAC", Status = _currentPolicies.IsUacEnabled ? "Enabled" : "Disabled", Recommendation = _currentPolicies.IsUacEnabled ? "Good" : "Enable UAC for better security" },
                new { PolicyType = "System", PolicyName = "RDP", Status = _currentPolicies.IsRdpEnabled ? "Enabled" : "Disabled", Recommendation = !_currentPolicies.IsRdpEnabled ? "Good" : "Consider disabling RDP if not needed" },
                new { PolicyType = "Account", PolicyName = "Guest Account", Status = _currentPolicies.IsGuestEnabled ? "Enabled" : "Disabled", Recommendation = !_currentPolicies.IsGuestEnabled ? "Good" : "Disable guest account" },
                new { PolicyType = "Account", PolicyName = "Administrator Account", Status = _currentPolicies.IsAdministratorEnabled ? "Enabled" : "Disabled", Recommendation = !_currentPolicies.IsAdministratorEnabled ? "Good" : "Consider disabling built-in admin" },
                new { PolicyType = "Overall", PolicyName = "Security Score", Status = _currentPolicies.SecurityScore.ToString(), Recommendation = GetScoreRecommendation(_currentPolicies.SecurityScore) }
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
                BuildViewFromPolicies(_currentPolicies);
            }
            catch (Exception ex)
            {
                SecurityScore = 0;
                SecurityScoreLabel = $"Hata: {ex.Message}";
                OnPropertyChanged(nameof(SecurityScore));
                OnPropertyChanged(nameof(SecurityScoreLabel));
            }
        }

        private void BuildViewFromPolicies(SecurityPolicies policies)
        {
            PasswordPolicies.Clear();
            SystemPolicies.Clear();
            AccountPolicies.Clear();
            Recommendations.Clear();
            CriticalIssues.Clear();
            WarningIssues.Clear();
            PassedChecks.Clear();

            PasswordPolicies.Add(new PolicyItem { Title = "Parola Gerekli", Description = "Kullanıcıların parola belirlemesi zorunlu", StatusText = policies.IsPasswordRequired ? "Evet" : "Hayır", IsGood = policies.IsPasswordRequired, EditKey = "PasswordRequired" });
            PasswordPolicies.Add(new PolicyItem { Title = "Minimum Parola Uzunluğu", Description = "Parolanın minimum karakter sayısı", StatusText = policies.MinPasswordLength.ToString(), IsGood = policies.MinPasswordLength >= 8, EditKey = "MinPasswordLength" });
            PasswordPolicies.Add(new PolicyItem { Title = "Karmaşık Parola Gereksinimi", Description = "Büyük/küçük harf, sayı ve özel karakter", StatusText = policies.IsPasswordComplexityEnabled ? "Açık" : "Kapalı", IsGood = policies.IsPasswordComplexityEnabled, EditKey = "PasswordComplexity" });

            SystemPolicies.Add(new PolicyItem { Title = "UAC (Kullanıcı Hesabı Denetimi)", Description = "Yönetici izinleri için onay isteme", StatusText = policies.IsUacEnabled ? "Etkin" : "Devre Dışı", IsGood = policies.IsUacEnabled, EditKey = "UAC" });
            SystemPolicies.Add(new PolicyItem { Title = "Uzak Masaüstü (RDP)", Description = "Uzaktan bağlantı imkanı", StatusText = policies.IsRdpEnabled ? "Açık" : "Kapalı", IsGood = !policies.IsRdpEnabled, EditKey = "RDP" });

            AccountPolicies.Add(new PolicyItem { Title = "Guest Hesabı", Description = "Konuk kullanıcı hesabının durumu", StatusText = policies.IsGuestEnabled ? "Etkin" : "Devre Dışı", IsGood = !policies.IsGuestEnabled, EditKey = "Guest" });
            AccountPolicies.Add(new PolicyItem { Title = "Administrator Hesabı", Description = "Yerleşik yönetici hesabının durumu", StatusText = policies.IsAdministratorEnabled ? "Etkin" : "Devre Dışı", IsGood = !policies.IsAdministratorEnabled, EditKey = "Administrator" });

            SecurityScore = policies.SecurityScore;
            SecurityScoreLabel = policies.SecurityScore >= 80 ? "Mükemmel" : policies.SecurityScore >= 60 ? "İyi" : policies.SecurityScore >= 40 ? "Orta" : "Zayıf";
            OnPropertyChanged(nameof(SecurityScore));
            OnPropertyChanged(nameof(SecurityScoreLabel));

            foreach (var rec in _securityService.GetRecommendations(policies)) Recommendations.Add(rec);

            // Issues classification
            if (!policies.IsPasswordRequired) CriticalIssues.Add(new IssueItem { Title = "Parola zorunlu değil", Detail = "Tüm kullanıcılar için parola zorunluluğunu etkinleştirin", FixKey = "PasswordRequired" }); else PassedChecks.Add("Parola zorunluluğu etkin");
            if (policies.MinPasswordLength < 8) WarningIssues.Add(new IssueItem { Title = "Kısa parola", Detail = $"Minimum {policies.MinPasswordLength} karakter", FixKey = "MinPasswordLength" }); else PassedChecks.Add("Parola uzunluğu uygun");
            if (!policies.IsPasswordComplexityEnabled) WarningIssues.Add(new IssueItem { Title = "Karmaşık parola kapalı", Detail = "Harf, sayı ve özel karakter zorunlu değil", FixKey = "PasswordComplexity" }); else PassedChecks.Add("Karmaşık parola etkin");
            if (!policies.IsUacEnabled) CriticalIssues.Add(new IssueItem { Title = "UAC devre dışı", Detail = "Yönetici izinleri kontrol edilmiyor", FixKey = "UAC" }); else PassedChecks.Add("UAC etkin");
            if (policies.IsRdpEnabled) WarningIssues.Add(new IssueItem { Title = "RDP açık", Detail = "Gerekli değilse kapatın", FixKey = "RDP" }); else PassedChecks.Add("RDP kapalı");
            if (policies.IsGuestEnabled) CriticalIssues.Add(new IssueItem { Title = "Guest hesabı etkin", Detail = "Konuk hesabını devre dışı bırakın", FixKey = "Guest" }); else PassedChecks.Add("Guest kapalı");
            if (policies.IsAdministratorEnabled) WarningIssues.Add(new IssueItem { Title = "Yerleşik Administrator etkin", Detail = "Gerekli değilse kapatın", FixKey = "Administrator" }); else PassedChecks.Add("Yerleşik admin kapalı");
        }

        public event Action<bool, string> LoadingStateChanged;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        // Düzenleme butonları için genel olay işleyici
        private void PolicyEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                switch (key)
                {
                    case "PasswordRequired": EditPasswordRequired_Click(sender, e); break;
                    case "MinPasswordLength": EditMinPasswordLength_Click(sender, e); break;
                    case "PasswordComplexity": EditComplexPassword_Click(sender, e); break;
                    case "UAC": EditUac_Click(sender, e); break;
                    case "RDP": EditRdp_Click(sender, e); break;
                    case "Guest": EditGuestAccount_Click(sender, e); break;
                    case "Administrator": EditAdministratorAccount_Click(sender, e); break;
                }
            }
        }

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

    public class PolicyItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string StatusText { get; set; }
        public bool IsGood { get; set; }
        public string EditKey { get; set; }
    }

    public class IssueItem
    {
        public string Title { get; set; }
        public string Detail { get; set; }
        public string FixKey { get; set; }
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
                policies.SecurityScore = 0;
            }

            return policies;
        }

        public List<string> GetRecommendations(SecurityPolicies policies)
        {
            var recommendations = new List<string>();

            if (!policies.IsPasswordRequired) recommendations.Add("Tüm kullanıcılar için parola zorunluluğu etkinleştirin");
            if (policies.MinPasswordLength < 8) recommendations.Add("Minimum parola uzunluğunu en az 8 karakter yapın");
            if (!policies.IsPasswordComplexityEnabled) recommendations.Add("Karmaşık parola gereksinimini etkinleştirin");
            if (!policies.IsUacEnabled) recommendations.Add("UAC (Kullanıcı Hesabı Denetimi) özelliğini etkinleştirin");
            if (policies.IsRdpEnabled) recommendations.Add("Gerekli değilse RDP (Uzak Masaüstü) özelliğini devre dışı bırakın");
            if (policies.IsGuestEnabled) recommendations.Add("Guest (Konuk) hesabını devre dışı bırakın");
            if (policies.IsAdministratorEnabled) recommendations.Add("Yerleşik Administrator hesabını devre dışı bırakın");
            if (recommendations.Count == 0) recommendations.Add("Güvenlik yapılandırmanız mükemmel görünüyor!");

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
                using var searcher = new ManagementObjectSearcher("SELECT PasswordRequired, Disabled FROM Win32_UserAccount WHERE LocalAccount = TRUE");
                foreach (ManagementObject obj in searcher.Get())
                {
                    bool disabled = obj["Disabled"] is bool d && d;
                    if (!disabled)
                    {
                        bool requiresPassword = obj["PasswordRequired"] is bool pr && pr;
                        if (!requiresPassword) return false;
                    }
                }
                return true; // all enabled local accounts require password
            }
            catch { }

            // Fallback: if WMI fails, infer from minimum length
            try
            {
                return GetMinimumPasswordLength() > 0;
            }
            catch { return false; }
        }
        private int GetMinimumPasswordLength()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa");
                var v = key?.GetValue("MinimumPasswordLength");
                if (v != null) return Convert.ToInt32(v);
            }
            catch { }
            try
            {
                using var key2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                var v2 = key2?.GetValue("MinimumPasswordLength");
                return v2 != null ? Convert.ToInt32(v2) : 0;
            }
            catch { return 0; }
        }
        private bool IsPasswordComplexityEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa");
                var v = key?.GetValue("PasswordComplexity");
                if (v != null) return Convert.ToInt32(v) == 1;
            }
            catch { }
            try
            {
                using var key2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                var v2 = key2?.GetValue("PasswordComplexity");
                return v2 != null && Convert.ToInt32(v2) == 1;
            }
            catch { return false; }
        }
        private bool IsUacEnabled()
        {
            try { using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"); return key?.GetValue("EnableLUA")?.ToString() == "1"; } catch { return false; }
        }
        private bool IsRdpEnabled()
        {
            try { using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server"); return key?.GetValue("fDenyTSConnections")?.ToString() == "0"; } catch { return false; }
        }
        private bool IsAccountEnabled(string userName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT Disabled FROM Win32_UserAccount WHERE Name = '{userName}' AND LocalAccount = TRUE");
                foreach (ManagementObject obj in searcher.Get()) { return !(bool)obj["Disabled"]; }
            }
            catch { }
            return false;
        }
    }
}
