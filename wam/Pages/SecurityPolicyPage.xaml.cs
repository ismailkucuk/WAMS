using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Controls;

namespace wam.Pages
{
    public partial class SecurityPolicyPage : UserControl
    {
        public SecurityPolicyPage()
        {
            InitializeComponent();
            LoadSecurityPolicies();
        }

        private void LoadSecurityPolicies()
        {
            try
            {
                int score = 0;
                if (IsPasswordRequired()) score += 20;
                if (GetMinimumPasswordLength() >= 8) score += 15;
                if (IsPasswordComplexityEnabled()) score += 15;
                if (IsUacEnabled()) score += 15;
                if (!IsRdpEnabled()) score += 15;
                if (!IsAccountEnabled("Guest")) score += 10;
                if (!IsAccountEnabled("Administrator")) score += 10;

                PasswordRequiredText.Text = $"🔐 Parola Gerekli: {(IsPasswordRequired() ? "Evet" : "Hayır")}";
                MinPasswordLengthText.Text = $"📏 Minimum Parola Uzunluğu: {GetMinimumPasswordLength()}";
                ComplexPasswordText.Text = $"🔢 Karmaşık Parola Gereksinimi: {(IsPasswordComplexityEnabled() ? "Açık" : "Kapalı")}";
                UacStatusText.Text = $"🔒 UAC Durumu: {(IsUacEnabled() ? "Etkin" : "Devre Dışı")}";
                RdpStatusText.Text = $"🌐 Uzak Masaüstü (RDP): {(IsRdpEnabled() ? "Açık" : "Kapalı")}";
                GuestStatusText.Text = $"👤 Guest Hesabı Durumu: {(IsAccountEnabled("Guest") ? "Etkin" : "Devre Dışı")}";
                AdministratorStatusText.Text = $"👑 Administrator Hesabı Durumu: {(IsAccountEnabled("Administrator") ? "Etkin" : "Devre Dışı")}";
                SecurityScoreText.Text = $"🔐 Güvenlik Skoru: {score}/100";

            }
            catch (Exception ex)
            {
                PasswordRequiredText.Text = $"Hata: {ex.Message}";
            }
        }

        private bool IsPasswordRequired()
        {
            return GetLocalSecuritySetting("PasswordPolicy", "PasswordHistorySize") != null;
        }

        private int GetMinimumPasswordLength()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa");
                var value = key?.GetValue("MinimumPasswordLength");
                return value != null ? Convert.ToInt32(value) : 0;
            }
            catch
            {
                return 0;
            }
        }


        private bool IsPasswordComplexityEnabled()
        {
            var value = GetLocalSecuritySetting("PasswordPolicy", "PasswordComplexity");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private bool IsUacEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                return key?.GetValue("EnableLUA")?.ToString() == "1";
            }
            catch { return false; }
        }

        private bool IsRdpEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server");
                return key?.GetValue("fDenyTSConnections")?.ToString() == "0";
            }
            catch { return false; }
        }

        private bool IsAccountEnabled(string userName)
        {
            try
            {
                var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_UserAccount WHERE Name = '{userName}' AND LocalAccount = TRUE");

                foreach (ManagementObject obj in searcher.Get())
                {
                    return (bool)obj["Disabled"] == false;
                }
            }
            catch { }

            return false;
        }

        private object GetLocalSecuritySetting(string category, string settingName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_SecuritySetting");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj.ClassPath.ClassName.Contains(category, StringComparison.OrdinalIgnoreCase))
                    {
                        return obj[settingName];
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
