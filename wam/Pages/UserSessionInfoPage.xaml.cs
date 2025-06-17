using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using wam.Services;

namespace wam.Pages
{
    public partial class UserSessionInfoPage : UserControl, ILoadablePage
    {
        private readonly UserSessionInfoViewModel _viewModel;

        public UserSessionInfoPage()
        {
            InitializeComponent();
            _viewModel = new UserSessionInfoViewModel();
            this.DataContext = _viewModel;
            
            // Export control'ü bu sayfaya bağla
            ExportControl.TargetPage = this;
            
            // ViewModel'den gelen olayı MainWindow'a iletmek için bir köprü kuruyoruz
            _viewModel.LoadingStateChanged += (isLoading, message) => LoadingStateChanged?.Invoke(isLoading, message);
        }

        public async Task LoadDataAsync()
        {
            await _viewModel.LoadAllDataAsync();
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = new
            {
                CurrentUser = new
                {
                    Name = _viewModel.CurrentUser.Name,
                    Role = _viewModel.CurrentUser.Role,
                    LastLogon = _viewModel.CurrentUser.LastLogon,
                    Initial = _viewModel.CurrentUser.Initial
                },
                LocalUsers = _viewModel.AllLocalUsers.Select(u => new
                {
                    Name = u.Name,
                    FullName = u.FullName,
                    Sid = u.Sid,
                    Status = u.Status,
                    IsAdmin = u.IsAdmin,
                    Groups = u.Groups
                }).ToList()
            };

            ExportService.ExportToJson(new[] { exportData }, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = _viewModel.AllLocalUsers.Select(u => new
            {
                Name = u.Name,
                FullName = u.FullName,
                Sid = u.Sid,
                Status = u.Status,
                IsAdmin = u.IsAdmin,
                IsCurrentUser = u.Name.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase),
                Groups = string.Join("; ", u.Groups ?? new List<string>())
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = new
            {
                CurrentUser = new
                {
                    Name = _viewModel.CurrentUser.Name,
                    Role = _viewModel.CurrentUser.Role,
                    LastLogon = _viewModel.CurrentUser.LastLogon,
                    Initial = _viewModel.CurrentUser.Initial
                },
                LocalUsers = _viewModel.AllLocalUsers.Select(u => new
                {
                    Name = u.Name,
                    FullName = u.FullName,
                    Sid = u.Sid,
                    Status = u.Status,
                    IsAdmin = u.IsAdmin,
                    Groups = u.Groups
                }).ToList()
            };

            ExportService.AutoExport(new[] { exportData }, GetModuleName());
        }

        public string GetModuleName()
        {
            return "UserSessionInfo";
        }

        // --- Yeni Kullanıcı Ekle Buton Click Event ---
        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddUserWindow();
            if (dialog.ShowDialog() == true)
            {
                if (this.DataContext is UserSessionInfoViewModel vm)
                {
                    vm.AddNewLocalUser(dialog.NewUserName, dialog.NewPassword);
                }
            }
        }

        public event Action<bool, string> LoadingStateChanged;
    }

    // --- ViewModel Sınıfları ---

    public class CurrentUserInfo : INotifyPropertyChanged
    {
        private string _name;
        private string _role;
        private string _lastLogon;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Initial)); } }
        public string Initial => !string.IsNullOrEmpty(Name) && Name.Contains("\\") ? Name.Split('\\')[1].Substring(0, 1).ToUpper() : (Name?.Length > 0 ? Name.Substring(0, 1).ToUpper() : "?");
        public string Role { get => _role; set { _role = value; OnPropertyChanged(); } }
        public string LastLogon { get => _lastLogon; set { _lastLogon = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class LocalUserViewModel : INotifyPropertyChanged
    {
        private string _status;
        private bool _isAdmin;
        private List<string> _groups;

        public string Name { get; set; }
        public string FullName { get; set; }
        public string Sid { get; set; }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public bool IsAdmin { get => _isAdmin; set { _isAdmin = value; OnPropertyChanged(); OnPropertyChanged(nameof(RoleIcon)); } }
        public string RoleIcon => IsAdmin ? "\uEA18" : "\uE77B"; // Kalkan ve kullanıcı ikonu
        public List<string> Groups { get => _groups; set { _groups = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class UserSessionInfoViewModel : INotifyPropertyChanged
    {
        public CurrentUserInfo CurrentUser { get; set; } = new CurrentUserInfo();
        public ObservableCollection<LocalUserViewModel> AllLocalUsers { get; } = new ObservableCollection<LocalUserViewModel>();

        private LocalUserViewModel _selectedUser;
        public LocalUserViewModel SelectedUser { get => _selectedUser; set { _selectedUser = value; OnPropertyChanged(); } }

        public ICommand ResetPasswordCommand { get; }
        public ICommand ToggleAccountStateCommand { get; }
        public ICommand ToggleAdminRoleCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public event Action<bool, string> LoadingStateChanged;

        public UserSessionInfoViewModel()
        {
            ResetPasswordCommand = new DelegateCommand(p => ResetPassword(p as LocalUserViewModel), p => p is LocalUserViewModel);
            ToggleAccountStateCommand = new DelegateCommand(p => ToggleAccountState(p as LocalUserViewModel), p => p is LocalUserViewModel);
            ToggleAdminRoleCommand = new DelegateCommand(p => ToggleAdminRole(p as LocalUserViewModel), p => p is LocalUserViewModel);
            DeleteUserCommand = new DelegateCommand(p => DeleteUser(p as LocalUserViewModel), p => p is LocalUserViewModel);
        }

        public async Task LoadAllDataAsync()
        {
            LoadingStateChanged?.Invoke(true, "Kullanıcı bilgileri yükleniyor...");

            var (currentUserInfo, allUsersList) = await Task.Run(() =>
            {
                var currentUser = GetCurrentUserInfoInternal();
                var allUsers = GetAllLocalUsersInternal();
                return (currentUser, allUsers);
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentUser = currentUserInfo;
                OnPropertyChanged(nameof(CurrentUser));
                AllLocalUsers.Clear();
                foreach (var user in allUsersList)
                {
                    AllLocalUsers.Add(user);
                }
                SelectedUser = AllLocalUsers.FirstOrDefault(u => u.Name.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase));
            });

            LoadingStateChanged?.Invoke(false, null);
        }

        public void AddNewLocalUser(string userName, string password)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    using (var user = new UserPrincipal(context))
                    {
                        user.SamAccountName = userName;
                        user.SetPassword(password);
                        user.Enabled = true;
                        user.Save();
                    }
                }
                MessageBox.Show("Kullanıcı başarıyla eklendi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcı eklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetPassword(LocalUserViewModel user)
        {
            if (user == null) return;
            var result = MessageBox.Show($"'{user.Name}' kullanıcısının parolası sıfırlanacak ve kullanıcı bir sonraki oturum açışında yeni bir parola belirlemek zorunda kalacak. Onaylıyor musunuz?", "Parola Sıfırlama Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    var userPrincipal = UserPrincipal.FindByIdentity(context, user.Name);
                    if (userPrincipal != null)
                    {
                        userPrincipal.ExpirePasswordNow();
                        userPrincipal.Save();
                        MessageBox.Show("Parola başarıyla sıfırlandı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"İşlem başarısız oldu (Yönetici izni gerekebilir):\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ToggleAccountState(LocalUserViewModel user)
        {
            if (user == null) return;
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    var userPrincipal = UserPrincipal.FindByIdentity(context, user.Name);
                    if (userPrincipal != null)
                    {
                        bool targetState = !(userPrincipal.Enabled ?? false);
                        userPrincipal.Enabled = targetState;
                        userPrincipal.Save();

                        user.Status = targetState ? "Aktif" : "Pasif";
                        MessageBox.Show($"'{user.Name}' hesabı başarıyla {(targetState ? "etkinleştirildi" : "devre dışı bırakıldı")}.", "Başarılı");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"İşlem başarısız oldu (Yönetici izni gerekebilir):\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ToggleAdminRole(LocalUserViewModel user)
        {
            if (user == null) return;
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    var userPrincipal = UserPrincipal.FindByIdentity(context, user.Name);
                    using (var group = GroupPrincipal.FindByIdentity(context, "Administrators"))
                    {
                        if (userPrincipal != null && group != null)
                        {
                            if (group.Members.Contains(userPrincipal))
                            {
                                group.Members.Remove(userPrincipal);
                                MessageBox.Show($"'{user.Name}' kullanıcısı Yönetici grubundan çıkarıldı.", "Başarılı");
                            }
                            else
                            {
                                group.Members.Add(userPrincipal);
                                MessageBox.Show($"'{user.Name}' kullanıcısı Yönetici grubuna eklendi.", "Başarılı");
                            }
                            group.Save();

                            user.IsAdmin = IsUserInAdminGroup(userPrincipal);
                            user.Groups = userPrincipal.GetGroups().Select(g => g.Name).ToList();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"İşlem başarısız oldu (Yönetici izni gerekebilir):\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private CurrentUserInfo GetCurrentUserInfoInternal()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return new CurrentUserInfo
                {
                    Name = identity.Name,
                    Role = principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Yönetici" : "Standart Kullanıcı",
                    LastLogon = GetLogonTimeInternal(identity.Name)
                };
            }
            catch { return new CurrentUserInfo { Name = "Bilgi Alınamadı" }; }
        }

        private void DeleteUser(LocalUserViewModel user)
        {
            if (user == null) return;
            if (user.Name.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Kendi oturumunuzu silemezsiniz!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"'{user.Name}' adlı kullanıcıyı silmek üzeresiniz. Devam etmek istediğinize emin misiniz?", "Kullanıcıyı Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    var userPrincipal = UserPrincipal.FindByIdentity(context, user.Name);
                    if (userPrincipal != null)
                    {
                        userPrincipal.Delete();
                        MessageBox.Show("Kullanıcı başarıyla silindi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadAllDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcı silinemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<LocalUserViewModel> GetAllLocalUsersInternal()
        {
            var users = new List<LocalUserViewModel>();
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            if (result is UserPrincipal up)
                            {
                                users.Add(new LocalUserViewModel
                                {
                                    Name = up.SamAccountName,
                                    FullName = up.DisplayName,
                                    Sid = up.Sid?.ToString() ?? "-",
                                    Status = up.IsAccountLockedOut() ? "Kilitli" : (up.Enabled == true ? "Aktif" : "Pasif"),
                                    IsAdmin = IsUserInAdminGroup(up),
                                    Groups = up.GetGroups().Select(g => g.Name).ToList()
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return users.OrderBy(u => u.Name).ToList();
        }

        private bool IsUserInAdminGroup(UserPrincipal user)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                using (var group = GroupPrincipal.FindByIdentity(context, "Administrators"))
                {
                    if (group != null)
                    {
                        return user.IsMemberOf(group);
                    }
                }
            }
            catch { return false; }
            return false;
        }

        private string GetLogonTimeInternal(string targetUserName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogonSession WHERE LogonType = 2 OR LogonType = 10"))
                {
                    foreach (ManagementObject logon in searcher.Get().Cast<ManagementObject>().OrderByDescending(l => l["StartTime"]))
                    {
                        var logonId = logon["LogonId"]?.ToString();
                        var query = $"ASSOCIATORS OF {{Win32_LogonSession.LogonId={logonId}}} WHERE AssocClass=Win32_LoggedOnUser Role=Dependent";
                        using (var userSearcher = new ManagementObjectSearcher(query))
                        {
                            foreach (ManagementObject user in userSearcher.Get())
                            {
                                var name = user["Name"]?.ToString();
                                var domain = user["Domain"]?.ToString();
                                var fullUserName = $"{domain}\\{name}";

                                if (fullUserName.Equals(targetUserName, StringComparison.OrdinalIgnoreCase))
                                {
                                    string rawTime = logon["StartTime"]?.ToString();
                                    if (!string.IsNullOrEmpty(rawTime))
                                    {
                                        return ManagementDateTimeConverter.ToDateTime(rawTime).ToString("dd.MM.yyyy HH:mm:ss");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return "Bilinmiyor";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // DelegateCommand örneği
    public class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    // Status için renk converter (Ellipse için)
    public class UserStatusToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch (value as string)
            {
                case "Aktif": return new SolidColorBrush(Color.FromRgb(40, 167, 69));
                case "Kilitli": return new SolidColorBrush(Color.FromRgb(220, 53, 69));
                case "Pasif": return new SolidColorBrush(Color.FromRgb(173, 181, 189));
                default: return new SolidColorBrush(Color.FromRgb(173, 181, 189));
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    // Admin için renk converter (Rol iconu için)
    public class AdminStatusToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool b && b) ? new SolidColorBrush(Color.FromRgb(74, 144, 226)) : new SolidColorBrush(Color.FromRgb(108, 117, 125));
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    // Bool to Text converter
    public class BoolToTextConverter : System.Windows.Data.IValueConverter
    {
        public static BoolToTextConverter Instance { get; } = new BoolToTextConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                var parts = paramString.Split(';');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
