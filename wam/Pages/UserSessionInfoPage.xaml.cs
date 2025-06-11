using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Management;
using System.Security.Principal;
using System.Windows.Controls;

namespace wam.Pages
{
    public partial class UserSessionInfoPage : UserControl
    {
        public UserSessionInfoPage()
        {
            InitializeComponent();
            LoadCurrentUserInfo();
            LoadAllLocalUsers();
        }

        private void LoadCurrentUserInfo()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                CurrentUserText.Text = $"Kullanıcı Adı: {identity.Name}";
                UserSidText.Text = $"SID: {identity.User.Value}";
                IsAdminText.Text = $"Yönetici Yetkisi: {(principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Evet" : "Hayır")}";
                LoginDomainText.Text = $"Giriş Yapılan Domain/Makine: {Environment.UserDomainName}";
                LoginTimeText.Text = $"Son Giriş Zamanı: {GetLogonTime()}";
            }
            catch (Exception ex)
            {
                CurrentUserText.Text = $"Bilgi alınamadı: {ex.Message}";
            }
        }

        private void LoadAllLocalUsers()
        {
            var users = new List<UserInfo>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            var de = result as UserPrincipal;
                            if (de != null)
                            {
                                users.Add(new UserInfo
                                {
                                    Name = de.SamAccountName,
                                    Sid = de.Sid?.ToString() ?? "-",
                                    IsAdmin = IsUserInAdminGroup(de.SamAccountName) ? "Evet" : "Hayır",
                                    IsActive = de.Enabled.HasValue && de.Enabled.Value ? "Evet" : "Hayır"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                users.Add(new UserInfo { Name = $"Hata: {ex.Message}" });
            }

            UsersDataGrid.ItemsSource = users;
        }

        private string GetLogonTime()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogonSession WHERE LogonType = 2 OR LogonType = 10"))
                {
                    foreach (ManagementObject logon in searcher.Get())
                    {
                        var logonId = logon["LogonId"]?.ToString();

                        using (var userSearcher = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_LogonSession.LogonId={logonId}}} WHERE AssocClass=Win32_LoggedOnUser Role=Dependent"))
                        {
                            foreach (ManagementObject user in userSearcher.Get())
                            {
                                var name = user["Name"]?.ToString();
                                if (name != null && name.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase))
                                {
                                    string rawTime = logon["StartTime"]?.ToString();
                                    if (!string.IsNullOrEmpty(rawTime))
                                    {
                                        var dt = ManagementDateTimeConverter.ToDateTime(rawTime);
                                        return dt.ToString("dd.MM.yyyy HH:mm:ss");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return "-";
        }


        private bool IsUserInAdminGroup(string userName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_GroupUser"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var partComponent = obj["PartComponent"]?.ToString();
                        var groupComponent = obj["GroupComponent"]?.ToString();

                        if (groupComponent != null && groupComponent.Contains("Name=\"Administrators\"") &&
                            partComponent != null && partComponent.Contains($"Name=\"{userName}\""))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }



        public class UserInfo
        {
            public string Name { get; set; }
            public string Sid { get; set; }
            public string IsAdmin { get; set; }
            public string IsActive { get; set; }
        }
    }
}
