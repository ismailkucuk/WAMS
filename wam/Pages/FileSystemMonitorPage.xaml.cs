using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace wam.Pages
{
    public partial class FileSystemMonitorPage : UserControl
    {
        private FileSystemWatcher watcher;
        private ObservableCollection<FileChangeEntry> logs = new ObservableCollection<FileChangeEntry>();

        public FileSystemMonitorPage()
        {
            InitializeComponent();
            LogList.ItemsSource = logs;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new VistaFolderBrowserDialog();
                if (dialog.ShowDialog() == true)
                {
                    TxtPath.Text = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}");
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string path = TxtPath.Text.Trim();

            if (!Directory.Exists(path))
            {
                MessageBox.Show("Geçerli bir klasör yolu girin.");
                return;
            }

            StopWatcher();        // varsa durdur
            logs.Clear();         // logları temizle 🧼

            watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
            };

            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Changed += OnChanged;
            watcher.Renamed += OnRenamed;

            logs.Insert(0, new FileChangeEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                ChangeType = "İzleme Başladı",
                FileName = path
            });
        }


        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopWatcher();
            logs.Insert(0, new FileChangeEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                ChangeType = "İzleme Durduruldu",
                FileName = TxtPath.Text
            });
        }

        private void StopWatcher()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                logs.Insert(0, new FileChangeEntry
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    ChangeType = e.ChangeType.ToString(),
                    FileName = e.FullPath
                });
            });
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                logs.Insert(0, new FileChangeEntry
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    ChangeType = "Yeniden Adlandırıldı",
                    FileName = $"{e.OldFullPath} ➜ {e.FullPath}"
                });
            });
        }
    }

    public class FileChangeEntry
    {
        public string Time { get; set; }
        public string ChangeType { get; set; }
        public string FileName { get; set; }
    }
}
