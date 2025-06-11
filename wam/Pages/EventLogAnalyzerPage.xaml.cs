using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Linq;
using System.Windows;

namespace wam.Pages
{
    public partial class EventLogAnalyzerPage : UserControl
    {
        public ObservableCollection<EventLogEntryDisplay> EventLogEntries { get; set; } = new ObservableCollection<EventLogEntryDisplay>();

        public EventLogAnalyzerPage()
        {
            InitializeComponent();
            EventLogDataGrid.ItemsSource = EventLogEntries;

            // Buraya LoadEventLog("Application"); gibi bir çağrı yapmıyoruz.
            // XAML'deki ApplicationLogRadioButton'ın IsChecked="True" olması,
            // InitializeComponent sonrası otomatik olarak LogType_Checked olayını tetikleyecektir.
            // Bu olay zaten LoadEventLog metodunu çağırır.
        }

        private void LoadEventLog(string logName)
        {
            EventLogEntries.Clear();

            try
            {
                EventLog log = new EventLog(logName);

                if (log.Entries.Count == 0)
                {
                    MessageBox.Show($"'{logName}' olay günlüğünde hiç giriş bulunmuyor.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var entries = log.Entries.Cast<EventLogEntry>()
                                   .OrderByDescending(entry => entry.TimeGenerated)
                                   .Take(2000);

                foreach (EventLogEntry entry in entries)
                {
                    EventLogEntries.Add(new EventLogEntryDisplay(entry));
                }
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show($"'{logName}' olay günlüğüne erişim reddedildi. Güvenlik günlüğünü görüntülemek için uygulamayı yönetici olarak çalıştırmanız gerekebilir.", "Erişim Reddedildi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Olay günlüğü yüklenirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogType_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            // Bu kontrol çok önemlidir. Eğer sender bir RadioButton değilse veya null ise hata vermesini engeller.
            if (rb != null && rb.IsChecked == true)
            {
                // rb.Tag null ise veya ToString() atılamaz ise hata verir.
                // Bu yüzden Tag'i null kontrolü ile kullanıyoruz.
                string logName = rb.Tag?.ToString(); // Null-conditional operator (?) eklendi
                if (!string.IsNullOrEmpty(logName))
                {
                    LoadEventLog(logName);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            string currentLogName = "Application";

            // RadioButton'lara x:Name verdiğimiz için doğrudan erişebiliriz.
            // Null kontrolü, bu RadioButton'lar XAML'de tanımlanmamışsa veya yüklenmemişse faydalıdır.
            if (ApplicationLogRadioButton != null && ApplicationLogRadioButton.IsChecked == true)
            {
                currentLogName = "Application";
            }
            else if (SystemLogRadioButton != null && SystemLogRadioButton.IsChecked == true)
            {
                currentLogName = "System";
            }
            else if (SecurityLogRadioButton != null && SecurityLogRadioButton.IsChecked == true)
            {
                currentLogName = "Security";
            }

            LoadEventLog(currentLogName);
        }

        public class EventLogEntryDisplay
        {
            private readonly EventLogEntry _entry;

            public EventLogEntryDisplay(EventLogEntry entry)
            {
                _entry = entry;
            }

            public DateTime TimeGenerated => _entry.TimeGenerated;
            public string TimeGeneratedFormatted => _entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss");
            public string Source => _entry.Source;
            public EventLogEntryType EntryType => _entry.EntryType;
            // EventID yerine InstanceId kullanılması önerilir. int'e dönüştürme hatası yaşanıyorsa string'e çevirerek gösterebilirsiniz.
            public int EventID => (int)_entry.InstanceId;
            public string Category => _entry.Category;
            public string Message => _entry.Message;
            public string MachineName => _entry.MachineName;
            public string UserName => string.IsNullOrEmpty(_entry.UserName) ? "N/A" : _entry.UserName;
        }
    }
}