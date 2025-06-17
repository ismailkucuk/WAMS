using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Task için eklendi
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using wam.Services;

namespace wam.Pages
{
    // 1. DEĞİŞİKLİK: ILoadablePage arayüzünü uyguluyoruz.
    public partial class ActiveAppMonitorPage : UserControl, IDisposable, ILoadablePage
    {
        private readonly System.Timers.Timer _pollingTimer;
        public ObservableCollection<ProcessInfoViewModel> RunningProcesses { get; set; }

        public ActiveAppMonitorPage()
        {
            InitializeComponent();
            RunningProcesses = new ObservableCollection<ProcessInfoViewModel>();
            AppActivityGrid.ItemsSource = RunningProcesses;

            // Olaylar (Events) burada tanımlanmaya devam edebilir.
            SortFieldCombo.SelectionChanged += (s, e) => ApplySorting();
            SortDirectionButton.Checked += (s, e) => ApplySorting();
            SortDirectionButton.Unchecked += (s, e) => ApplySorting();
            AppActivityGrid.PreviewMouseWheel += AppActivityGrid_PreviewMouseWheel;

            // 2. DEĞİŞİKLİK: İlk veri yükleme çağrısını constructor'dan kaldırdık.
            // Bu artık LoadDataAsync metodu tarafından yönetilecek.
            // RefreshProcessList(null, null); // BU SATIR KALDIRILDI

            _pollingTimer = new System.Timers.Timer(2500);
            _pollingTimer.Elapsed += RefreshProcessList;
            _pollingTimer.Start();

            this.Unloaded += ActiveAppMonitorPage_Unloaded;
            
            // Export control'ü bu sayfaya bağla
            ExportControl.TargetPage = this;
        }

        // 3. DEĞİŞİKLİK: Yeni, asenkron veri yükleme metodunu oluşturduk.
        // MainWindow bu metodu çağırıp işlemin bitmesini bekleyecek.
        public async Task LoadDataAsync()
        {
            // İlk yüklemenin hızlıca gerçekleşmesi için RefreshProcessList'in mantığını
            // burada asenkron olarak çalıştırıyoruz.
            await Task.Run(() => RefreshProcessList(null, null));
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = RunningProcesses.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                MemoryUsage = p.MemoryUsage,
                MemoryUsageFormatted = p.MemoryUsageFormatted,
                CpuUsage = p.CpuUsage.TotalMilliseconds,
                CpuUsageFormatted = p.CpuUsageFormatted,
                StartTime = p.StartTime,
                StartTimeFormatted = p.StartTimeFormatted,
                WindowTitle = p.WindowTitle
            }).ToList();

            ExportService.ExportToJson(exportData, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = RunningProcesses.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                MemoryUsageMB = Math.Round(p.MemoryUsage / (1024.0 * 1024.0), 2),
                CpuUsageSeconds = (int)p.CpuUsage.TotalSeconds,
                StartTime = p.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                WindowTitle = p.WindowTitle
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = RunningProcesses.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                MemoryUsage = p.MemoryUsage,
                MemoryUsageFormatted = p.MemoryUsageFormatted,
                CpuUsage = p.CpuUsage.TotalMilliseconds,
                CpuUsageFormatted = p.CpuUsageFormatted,
                StartTime = p.StartTime,
                StartTimeFormatted = p.StartTimeFormatted,
                WindowTitle = p.WindowTitle
            }).ToList();

            ExportService.AutoExport(exportData, GetModuleName());
        }

        public string GetModuleName()
        {
            return "ActiveAppMonitor";
        }

        // Yavaş scroll için eklenen handler (Değişiklik yok)
        private void AppActivityGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(AppActivityGrid);
            if (scrollViewer != null)
            {
                double scrollAmount = e.Delta / 5.0;
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - scrollAmount);
                e.Handled = true;
            }
        }

        // VisualTree'de ScrollViewer bulucu (Değişiklik yok)
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T tChild)
                    return tChild;
                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void ActiveAppMonitorPage_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            _pollingTimer?.Stop();
            _pollingTimer?.Dispose();
        }

        // ApplySorting ve RefreshProcessList metodları ve ProcessInfoViewModel sınıfı
        // aynı şekilde kalabilir, onlarda değişiklik yapmaya gerek yok.
        // ... (Mevcut ApplySorting, RefreshProcessList ve ProcessInfoViewModel kodların burada yer alacak) ...
        private void ApplySorting()
        {
            if (RunningProcesses == null || RunningProcesses.Count <= 1) return;

            string sortField = ((SortFieldCombo.SelectedItem as ComboBoxItem)?.Tag as string) ?? "Name";
            bool descending = SortDirectionButton.IsChecked == true;

            List<ProcessInfoViewModel> sortedList = sortField switch
            {
                "Id" => descending ? RunningProcesses.OrderByDescending(p => p.Id).ToList() : RunningProcesses.OrderBy(p => p.Id).ToList(),
                "MemoryUsage" => descending ? RunningProcesses.OrderByDescending(p => p.MemoryUsage).ToList() : RunningProcesses.OrderBy(p => p.MemoryUsage).ToList(),
                "CpuUsage" => descending ? RunningProcesses.OrderByDescending(p => p.CpuUsage.TotalMilliseconds).ToList() : RunningProcesses.OrderBy(p => p.CpuUsage.TotalMilliseconds).ToList(),
                "Name" or _ => descending ? RunningProcesses.OrderByDescending(p => p.Name).ToList() : RunningProcesses.OrderBy(p => p.Name).ToList(),
            };

            for (int i = 0; i < sortedList.Count; i++)
            {
                var item = sortedList[i];
                int oldIndex = RunningProcesses.IndexOf(item);
                if (oldIndex != i)
                {
                    RunningProcesses.Move(oldIndex, i);
                }
            }
        }

        private void RefreshProcessList(object sender, ElapsedEventArgs e)
        {
            var latestProcesses = new Dictionary<int, ProcessInfoViewModel>();
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        if (latestProcesses.ContainsKey(p.Id)) continue;
                        latestProcesses.Add(p.Id, new ProcessInfoViewModel
                        {
                            Id = p.Id,
                            Name = p.ProcessName,
                            MemoryUsage = p.WorkingSet64,
                            StartTime = p.StartTime,
                            WindowTitle = p.MainWindowHandle != IntPtr.Zero ? p.MainWindowTitle : "N/A",
                            CpuUsage = p.TotalProcessorTime
                        });
                    }
                    catch { }
                }
            }
            catch { }

            Dispatcher.Invoke(() =>
            {
                var existingPids = new HashSet<int>(RunningProcesses.Select(p => p.Id));
                var latestPids = new HashSet<int>(latestProcesses.Keys);

                var processesToRemove = RunningProcesses.Where(p => !latestPids.Contains(p.Id)).ToList();
                foreach (var p in processesToRemove) { RunningProcesses.Remove(p); }

                foreach (var process in RunningProcesses)
                {
                    if (latestProcesses.TryGetValue(process.Id, out var updatedProcess))
                    {
                        process.MemoryUsage = updatedProcess.MemoryUsage;
                        process.CpuUsage = updatedProcess.CpuUsage;
                        process.WindowTitle = updatedProcess.WindowTitle;
                    }
                }

                var processesToAdd = latestProcesses.Values.Where(p => !existingPids.Contains(p.Id)).ToList();
                foreach (var p in processesToAdd) { RunningProcesses.Add(p); }

                ApplySorting();
            });
        }
    }

    // ProcessInfoViewModel sınıfı burada aynı şekilde kalacak
    public class ProcessInfoViewModel : INotifyPropertyChanged
    {
        private string _windowTitle;
        private long _memoryUsage;
        private TimeSpan _cpuUsage;

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime StartTime { get; set; }

        public string WindowTitle
        {
            get => string.IsNullOrEmpty(_windowTitle) ? "N/A" : _windowTitle;
            set { if (_windowTitle != value) { _windowTitle = value; OnPropertyChanged(); } }
        }

        public long MemoryUsage
        {
            get => _memoryUsage;
            set { if (_memoryUsage != value) { _memoryUsage = value; OnPropertyChanged(nameof(MemoryUsageFormatted)); } }
        }

        public TimeSpan CpuUsage
        {
            get => _cpuUsage;
            set { if (_cpuUsage != value) { _cpuUsage = value; OnPropertyChanged(nameof(CpuUsageFormatted)); } }
        }

        public string MemoryUsageFormatted => $"{_memoryUsage / (1024.0 * 1024.0):F2} MB";
        public string CpuUsageFormatted => $"{(int)_cpuUsage.TotalSeconds}s";
        public string StartTimeFormatted => StartTime.ToString("HH:mm:ss");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}