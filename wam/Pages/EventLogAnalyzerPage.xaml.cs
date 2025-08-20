using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using wam.Controls;
using wam.Services;
using System.Security.Principal;

namespace wam.Pages
{
    public partial class EventLogAnalyzerPage : UserControl, ILoadablePage
    {
        private readonly EventLogAnalyzerViewModel _viewModel;

        public EventLogAnalyzerPage()
        {
            InitializeComponent();
            _viewModel = new EventLogAnalyzerViewModel();
            this.DataContext = _viewModel;
            // ViewModel'den gelen olayı MainWindow'a iletmek için bir köprü kuruyoruz
            _viewModel.LoadingStateChanged += (isLoading, message) => LoadingStateChanged?.Invoke(isLoading, message);
            
            // Export control'ü bu sayfaya bağla
            ExportControl.TargetPage = this;
        }

        public async Task LoadDataAsync()
        {
            // Güvenlik günlüğünü görüntülemek için yönetici gereklidir. İlk yüklemede Application'ı açıyoruz.
            await _viewModel.LoadEventsAsync("Application");
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = _viewModel.EventLogEntries.Select(e => new
            {
                TimeGenerated = e.TimeGeneratedFormatted,
                Source = e.Source,
                EntryType = e.EntryType,
                EventID = e.EventID,
                Category = e.Category,
                Message = e.Message,
                UserName = e.UserName,
                ParsedMessage = e.ParsedMessage?.Select(pm => new { Key = pm.Key, Value = pm.Value }).ToList()
            }).ToList();

            ExportService.ExportToJson(exportData, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = _viewModel.EventLogEntries.Select(e => new
            {
                TimeGenerated = e.TimeGeneratedFormatted,
                Source = e.Source,
                EntryType = e.EntryType,
                EventID = e.EventID,
                Category = e.Category,
                Message = e.Message.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " "), // CSV için tek satır
                UserName = e.UserName
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = _viewModel.EventLogEntries.Select(e => new
            {
                TimeGenerated = e.TimeGeneratedFormatted,
                Source = e.Source,
                EntryType = e.EntryType,
                EventID = e.EventID,
                Category = e.Category,
                Message = e.Message,
                UserName = e.UserName,
                ParsedMessage = e.ParsedMessage?.Select(pm => new { Key = pm.Key, Value = pm.Value }).ToList()
            }).ToList();

            ExportService.AutoExport(exportData, GetModuleName());
        }

        public string GetModuleName()
        {
            return "EventLogAnalyzer";
        }

        private async void LogType_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded && sender is RadioButton radioButton && radioButton.IsChecked == true)
            {
                string logName = radioButton.Tag?.ToString();
                if (!string.IsNullOrEmpty(logName))
                {
                    if (string.Equals(logName, "Security", StringComparison.OrdinalIgnoreCase) && !IsRunningAsAdmin())
                    {
                        var dlg = new wam.Dialogs.AdminRequiredDialog { Owner = Application.Current.MainWindow };
                        var goSettings = dlg.ShowDialog() == true;
                        if (goSettings && Application.Current.MainWindow is MainWindow mw)
                        {
                            await mw.NavigateToPage<SettingsPage>("Ayarlar");
                        }
                        // Admin değilse, güvenlik günlüğüne geçme.
                        return;
                    }

                    await _viewModel.LoadEventsAsync(logName);
                }
            }
        }

        // Liste öğesine tıklandığında detay penceresini açar
        private void ListViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is EventLogEntryViewModel vm)
            {
                var detailWindow = new EventDetailWindow(vm)
                {
                    Owner = Window.GetWindow(this)
                };
                detailWindow.ShowDialog();
            }
        }

        // Kaydırmayı yavaşlat!
        private void EventLogListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(EventLogListView);
            if (scrollViewer != null)
            {
                // Oran küçüldükçe kaydırma yavaşlar
                double scrollAmount = e.Delta * 0.7; // örn. 0.10-0.20 arası oynatabilirsin
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - scrollAmount);
                e.Handled = true;
            }
        }

        // Visual tree'den ScrollViewer bulmak için
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

    public class EventLogEntryViewModel
    {
        // Ham veriyi saklayalım
        private readonly EventLogEntry _entry;

        // Görüntülenecek özellikler
        public string TimeGeneratedFormatted => _entry.TimeGenerated.ToString("dd.MM.yyyy HH:mm:ss");
        public string Source => _entry.Source;
        public string EntryType => _entry.EntryType.ToString();
        public long EventID => _entry.InstanceId;
        public string Category => _entry.Category;
        public string Message => _entry.Message.Trim();
        public string UserName => string.IsNullOrEmpty(_entry.UserName) ? "N/A" : _entry.UserName;

        // Ayrıştırılmış mesaj için
        public List<KeyValuePair<string, string>> ParsedMessage { get; private set; }

        public EventLogEntryViewModel(EventLogEntry entry)
        {
            _entry = entry;
            ParseMessage();
        }

        // Mesajı ayrıştıran metot
        private void ParseMessage()
        {
            ParsedMessage = new List<KeyValuePair<string, string>>();
            var lines = Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string currentHeader = "Genel Mesaj";
            var messageContent = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                // "Konu:" gibi bir başlık mı? (Boşlukları ve sekmeleri de hesaba katalım)
                if (Regex.IsMatch(line.Trim(), @"^[\w\s\(\)]+:$"))
                {
                    // Önceki birikmiş içeriği ekle
                    if (messageContent.Length > 0)
                    {
                        ParsedMessage.Add(new KeyValuePair<string, string>(currentHeader.TrimEnd(':'), messageContent.ToString().Trim()));
                        messageContent.Clear();
                    }
                    currentHeader = line.Trim();
                }
                else
                {
                    messageContent.AppendLine(line.Trim());
                }
            }
            // Son kalan içeriği de ekle
            if (messageContent.Length > 0 || ParsedMessage.Count == 0)
            {
                ParsedMessage.Add(new KeyValuePair<string, string>(currentHeader.TrimEnd(':'), messageContent.ToString().Trim()));
            }
        }
    }

    public class EventLogAnalyzerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<EventLogEntryViewModel> EventLogEntries { get; } = new ObservableCollection<EventLogEntryViewModel>();
        public event Action<bool, string> LoadingStateChanged;

        public async Task LoadEventsAsync(string logName)
        {
            LoadingStateChanged?.Invoke(true, $"'{logName}' günlüğü okunuyor...");

            var entriesList = await Task.Run(() =>
            {
                var tempList = new List<EventLogEntryViewModel>();
                try
                {
                    if (!EventLog.Exists(logName))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show($"'{logName}' olay günlüğü bu sistemde bulunmuyor.", "Bulunamadı", MessageBoxButton.OK, MessageBoxImage.Information));
                        return tempList;
                    }

                    EventLog log = new EventLog(logName);

                    var entries = log.Entries.Cast<EventLogEntry>()
                                             .OrderByDescending(entry => entry.TimeGenerated)
                                             .Take(500)
                                             .ToList();

                    foreach (EventLogEntry entry in entries)
                    {
                        tempList.Add(new EventLogEntryViewModel(entry));
                    }
                }
                catch (System.Security.SecurityException)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"'{logName}' olay günlüğüne erişim reddedildi. Güvenlik günlüğünü görüntülemek için uygulamayı yönetici olarak çalıştırmanız gerekebilir.", "Erişim Reddedildi", MessageBoxButton.OK, MessageBoxImage.Warning));
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                       MessageBox.Show($"Olay günlüğü yüklenirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error));
                }
                return tempList;
            });

            EventLogEntries.Clear();
            foreach (var item in entriesList)
            {
                EventLogEntries.Add(item);
            }

            LoadingStateChanged?.Invoke(false, null);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
