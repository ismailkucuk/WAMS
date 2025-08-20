using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using wam.Services;
using System.Security.Principal;

namespace wam.Pages
{
    public partial class UserActivityPage : UserControl, ILoadablePage
    {
        private readonly UserActivityPageViewModel _viewModel;

        public UserActivityPage()
        {
            InitializeComponent();
            _viewModel = new UserActivityPageViewModel();
            this.DataContext = _viewModel;
            
            // Export control'ü bu sayfaya bağla
            ExportControl.TargetPage = this;
            
            _viewModel.LoadingStateChanged += (isLoading, message) => LoadingStateChanged?.Invoke(isLoading, message);
        }

        public async Task LoadDataAsync()
        {
            if (!IsRunningAsAdmin())
            {
                var dlg = new wam.Dialogs.AdminRequiredDialog { Owner = Application.Current.MainWindow };
                var goSettings = dlg.ShowDialog() == true;
                if (goSettings && Application.Current.MainWindow is MainWindow mw)
                {
                    await mw.NavigateToPage<SettingsPage>("Ayarlar");
                }
                return;
            }

            await _viewModel.LoadUserActivitiesAsync("All");
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = _viewModel.UserActivities.Select(a => new
            {
                TimeCreated = a.TimeCreated,
                EventType = a.EventType,
                UserName = a.UserName,
                EventId = a.EventId,
                LogonType = a.LogonType,
                SourceIpAddress = a.SourceIpAddress
            }).ToList();

            ExportService.ExportToJson(exportData, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = _viewModel.UserActivities.Select(a => new
            {
                TimeCreated = a.TimeCreated,
                EventType = a.EventType,
                UserName = a.UserName,
                EventId = a.EventId,
                LogonType = a.LogonType,
                SourceIpAddress = a.SourceIpAddress
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = _viewModel.UserActivities.Select(a => new
            {
                TimeCreated = a.TimeCreated,
                EventType = a.EventType,
                UserName = a.UserName,
                EventId = a.EventId,
                LogonType = a.LogonType,
                SourceIpAddress = a.SourceIpAddress
            }).ToList();

            ExportService.AutoExport(exportData, GetModuleName());
        }

        public string GetModuleName()
        {
            return "UserActivity";
        }

        private async void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded && sender is RadioButton radioButton && radioButton.IsChecked == true)
            {
                string filter = radioButton.Tag?.ToString() ?? "All";
                await _viewModel.LoadUserActivitiesAsync(filter);
            }
        }

        private void ListViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is UserActivityViewModel vm)
            {
                var detailWindow = new UserActivityDetailWindow(vm)
                {
                    Owner = Window.GetWindow(this)
                };
                detailWindow.ShowDialog();
            }
        }

        private void ActivityListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(ActivityListView);
            if (scrollViewer != null)
            {
                double duration = 0.4;
                double newOffset = scrollViewer.VerticalOffset - (e.Delta * 0.5);
                DoubleAnimation animation = new DoubleAnimation(newOffset, new Duration(TimeSpan.FromSeconds(duration)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var storyboard = new Storyboard();
                storyboard.Children.Add(animation);
                Storyboard.SetTarget(animation, scrollViewer);
                Storyboard.SetTargetProperty(animation, new PropertyPath(AnimatedScrollHelper.VerticalOffsetProperty));
                storyboard.Begin();
                e.Handled = true;
            }
        }

        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T tChild) return tChild;
                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        public event Action<bool, string> LoadingStateChanged;

        private static bool IsRunningAsAdmin()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }

    public class UserActivityViewModel
    {
        public string TimeCreated { get; set; }
        public string EventType { get; set; }
        public string UserName { get; set; }
        public int EventId { get; set; }
        public string LogonType { get; set; }
        public string SourceIpAddress { get; set; }
    }

    public class UserActivityPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<UserActivityViewModel> UserActivities { get; } = new ObservableCollection<UserActivityViewModel>();
        public event Action<bool, string> LoadingStateChanged;

        public async Task LoadUserActivitiesAsync(string filter)
        {
            LoadingStateChanged?.Invoke(true, "Kullanıcı aktiviteleri taranıyor...");

            var activities = await Task.Run(() =>
            {
                var tempList = new List<UserActivityViewModel>();
                var startTime = DateTime.Now.AddDays(-1); // Son 24 saat
                string timeFilter = $"TimeCreated[@SystemTime >= '{startTime.ToUniversalTime():o}']";

                string eventIdFilter = filter switch
                {
                    "LogonLogoff" => "(EventID=4624 or EventID=4634)",
                    "Critical" => "(EventID=4672 or EventID=4740)",
                    _ => "(EventID=4624 or EventID=4634 or EventID=4672 or EventID=4740)"
                };

                string query = $"*[System[{eventIdFilter} and {timeFilter}]]";

                try
                {
                    var logQuery = new EventLogQuery("Security", PathType.LogName, query) { ReverseDirection = true };
                    var reader = new EventLogReader(logQuery);
                    EventRecord record;
                    for (int i = 0; (record = reader.ReadEvent()) != null && i < 500; i++)
                    {
                        using (record)
                        {
                            tempList.Add(new UserActivityViewModel
                            {
                                TimeCreated = record.TimeCreated?.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") ?? "-",
                                EventType = GetEventType(record.Id),
                                UserName = GetUserNameFromRecord(record),
                                EventId = record.Id,
                                LogonType = GetLogonType(record),
                                SourceIpAddress = GetSourceIp(record)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Kullanıcı aktiviteleri okunurken bir hata oluştu (Yönetici izni gerekebilir):\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error));
                }
                return tempList;
            });

            UserActivities.Clear();
            foreach (var activity in activities)
            {
                UserActivities.Add(activity);
            }

            LoadingStateChanged?.Invoke(false, null);
        }

        private string GetEventType(int eventId) => eventId switch
        {
            4624 => "Oturum Açıldı",
            4634 => "Oturum Kapatıldı",
            4672 => "Özel Yetkilerle Oturum Açıldı (Yönetici)",
            4740 => "Hesap Kilitlendi",
            _ => $"Bilinmeyen Olay ({eventId})"
        };

        // YENİDEN YAZILDI: Her olay ID'si için doğru pozisyondan veri okuyan metot
        private string GetUserNameFromRecord(EventRecord record)
        {
            try
            {
                int index = record.Id switch
                {
                    4624 => 5, // Hesap Adı
                    4634 => 1, // Hesap Adı
                    4672 => 1, // Hesap Adı
                    4740 => 0, // Hedef Hesap Adı (Kilitlenen hesap)
                    _ => -1
                };
                return (index != -1 && record.Properties.Count > index) ? record.Properties[index].Value.ToString() : "N/A";
            }
            catch { return "N/A"; }
        }

        // YENİDEN YAZILDI: Her olay ID'si için doğru pozisyondan veri okuyan metot
        private string GetSourceIp(EventRecord record)
        {
            try
            {
                int index = record.Id switch
                {
                    4624 => 18, // Kaynak Ağ Adresi
                    _ => -1
                };
                return (index != -1 && record.Properties.Count > index) ? record.Properties[index].Value.ToString() : "-";
            }
            catch { return "-"; }
        }

        // YENİDEN YAZILDI: Her olay ID'si için doğru pozisyondan veri okuyan metot
        private string GetLogonType(EventRecord record)
        {
            try
            {
                int index = record.Id switch
                {
                    4624 => 8, // Oturum Açma Türü
                    4634 => 4, // Oturum Açma Türü
                    _ => -1
                };

                if (index == -1 || record.Properties.Count <= index) return "-";

                return Convert.ToInt32(record.Properties[index].Value) switch
                {
                    2 => "İnteraktif (Yerel Klavye/Ekran)",
                    3 => "Ağ (Paylaşılan klasör vb.)",
                    4 => "Toplu İş (Zamanlanmış görev vb.)",
                    5 => "Servis",
                    7 => "Kilit Açma",
                    8 => "Ağ (Şifre metin olarak)",
                    10 => "Uzak Masaüstü",
                    11 => "Uzak İnteraktif",
                    _ => $"Diğer ({record.Properties[index].Value})"
                };
            }
            catch { return "-"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class AnimatedScrollHelper
    {
        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(AnimatedScrollHelper), new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));
        public static void SetVerticalOffset(DependencyObject obj, double value) => obj.SetValue(VerticalOffsetProperty, value);
        public static double GetVerticalOffset(DependencyObject obj) => (double)obj.GetValue(VerticalOffsetProperty);
        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer) { scrollViewer.ScrollToVerticalOffset((double)e.NewValue); }
        }
    }
}
