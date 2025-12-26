using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; // SaveFileDialog için bu using gerekli
using Ookii.Dialogs.Wpf;
using wam.Services;

namespace wam.Pages
{
    public partial class FileSystemMonitorPage : UserControl, ILoadablePage
    {
        private readonly FileSystemMonitorViewModel _viewModel;

        public FileSystemMonitorPage()
        {
            InitializeComponent();
            _viewModel = new FileSystemMonitorViewModel();
            this.DataContext = _viewModel;
        }

        public async Task LoadDataAsync()
        {
            // FileSystem monitor başlangıç durumunu yükler
            await Task.CompletedTask; // Bu sayfa başlangıçta özel bir yükleme gerektirmiyor
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = new
            {
                MonitoringStatus = new
                {
                    IsMonitoring = _viewModel.IsMonitoring,
                    IsNotMonitoring = _viewModel.IsNotMonitoring,
                    HasLogs = _viewModel.HasLogs
                },
                FileSystemEvents = _viewModel.Logs.Select(log => new
                {
                    Time = log.Time,
                    ChangeType = log.ChangeType,
                    FileName = log.FileName
                }).ToList()
            };

            ExportService.ExportToJson(new[] { exportData }, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = _viewModel.Logs.Select(log => new
            {
                Time = log.Time,
                ChangeType = log.ChangeType,
                FileName = log.FileName,
                IsMonitoring = _viewModel.IsMonitoring
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = new
            {
                MonitoringStatus = new
                {
                    IsMonitoring = _viewModel.IsMonitoring,
                    IsNotMonitoring = _viewModel.IsNotMonitoring,
                    HasLogs = _viewModel.HasLogs
                },
                FileSystemEvents = _viewModel.Logs.Select(log => new
                {
                    Time = log.Time,
                    ChangeType = log.ChangeType,
                    FileName = log.FileName
                }).ToList()
            };

            ExportService.AutoExport(new[] { exportData }, GetModuleName());
        }

        public string GetModuleName()
        {
            return "FileSystemMonitor";
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
            _viewModel.StartWatching(TxtPath.Text);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StopWatching();
        }

        // YENİ: Dışa Aktar butonu için olay yöneticisi
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportLogs();
        }

        // Sütun genişliklerini dinamik olarak ayarlama
        private void LogList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView?.View is GridView gridView)
            {
                var totalWidth = listView.ActualWidth - 30; // ScrollBar için yer ayır
                
                // %25, %25, %50 oranlarında böl
                gridView.Columns[0].Width = totalWidth * 0.25; // Zaman
                gridView.Columns[1].Width = totalWidth * 0.25; // Olay Türü  
                gridView.Columns[2].Width = totalWidth * 0.50; // Dosya Yolu
            }
        }
    }

    public class FileChangeEntry
    {
        public string Time { get; set; }
        public string ChangeType { get; set; }
        public string FileName { get; set; }
    }

    public class FileSystemMonitorViewModel : INotifyPropertyChanged
    {
        private FileSystemWatcher _watcher;
        private bool _isMonitoring;

        public ObservableCollection<FileChangeEntry> Logs { get; } = new ObservableCollection<FileChangeEntry>();

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                if (_isMonitoring != value)
                {
                    _isMonitoring = value;
                    OnPropertyChanged(nameof(IsMonitoring));
                    OnPropertyChanged(nameof(IsNotMonitoring));
                }
            }
        }

        // YENİ: Log listesinin boş olup olmadığını kontrol eden özellik
        public bool HasLogs => Logs.Any();

        public FileSystemMonitorViewModel()
        {
            // Log koleksiyonu her değiştiğinde, HasLogs özelliğinin de
            // değiştiğini UI'a haber veriyoruz.
            Logs.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasLogs));
        }

        // YENİ: Logları dışa aktarma metodu
        public void ExportLogs()
        {
            if (!HasLogs)
            {
                MessageBox.Show("Dışa aktarılacak log bulunmuyor.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Metin Dosyası (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*";
            saveFileDialog.FileName = $"wam_dosya_log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Logları düzenli bir formatta bir araya getiriyoruz
                    var logLines = Logs.Select(log => $"{log.Time} | {log.ChangeType,-20} | {log.FileName}");
                    File.WriteAllLines(saveFileDialog.FileName, logLines);
                    MessageBox.Show($"Loglar başarıyla kaydedildi:\n{saveFileDialog.FileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Loglar kaydedilirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // XAML'de IsEnabled="{Binding IsNotMonitoring}" şeklinde kullanmak için
        public bool IsNotMonitoring => !IsMonitoring;

        public void StartWatching(string path)
        {
            if (IsMonitoring) return;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Lütfen geçerli bir klasör yolu seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StopWatching();
            Logs.Clear();

            _watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
            };

            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Changed += OnChanged;
            _watcher.Renamed += OnRenamed;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, new FileChangeEntry
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    ChangeType = "İzleme Başladı",
                    FileName = path
                });
            });

            IsMonitoring = true; // Durumu 'izleniyor' olarak ayarla, UI otomatik güncellenecek
        }

        public void StopWatching()
        {
            if (!IsMonitoring && _watcher == null) return;

            string watchedPath = _watcher?.Path;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            if (IsMonitoring)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Insert(0, new FileChangeEntry
                    {
                        Time = DateTime.Now.ToString("HH:mm:ss"),
                        ChangeType = "İzleme Durduruldu",
                        FileName = watchedPath ?? "N/A"
                    });
                });
            }

            IsMonitoring = false; // Durumu 'izlenmiyor' olarak ayarla, UI otomatik güncellenecek
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, new FileChangeEntry
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    ChangeType = e.ChangeType.ToString(),
                    FileName = e.FullPath
                });
            });
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, new FileChangeEntry
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    ChangeType = "Yeniden Adlandırıldı",
                    FileName = $"{e.OldFullPath}  ➜  {e.FullPath}"
                });
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}