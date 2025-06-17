using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Win32.TaskScheduler;
using wam.Services;

// HATA ÇÖZÜMÜ 1: İsim çakışmasını çözmek için takma ad (alias) oluşturuyoruz.
// Artık kod içinde "Task" yazdığımızda, derleyici bunun System.Threading.Tasks.Task olduğunu bilecek.
using Task = System.Threading.Tasks.Task;

namespace wam.Pages
{
    // HATA ÇÖZÜMÜ 2: ILoadablePage hatası, yukarıdaki takma ad ile otomatik olarak çözüldü.
    public partial class StartupProgramsPage : UserControl, ILoadablePage
    {
        private StartupProgramsViewModel _viewModel;

        public StartupProgramsPage()
        {
            InitializeComponent();
            _viewModel = new StartupProgramsViewModel();
            this.DataContext = _viewModel;
        }

        public async Task LoadDataAsync()
        {
            await _viewModel.LoadAllStartupItemsAsync();
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = new
            {
                StartupRegistryItems = _viewModel.StartupRegistryItems.Select(item => new
                {
                    Name = item.Name,
                    Path = item.Path,
                    Source = item.Source
                }).ToList(),
                ScheduledTasks = _viewModel.ScheduledTasks.Select(task => new
                {
                    Name = task.Name,
                    Action = task.Action,
                    Trigger = task.Trigger,
                    Author = task.Author,
                    LastRunTime = task.LastRunTime,
                    NextRunTime = task.NextRunTime
                }).ToList()
            };

            ExportService.ExportToJson(new[] { exportData }, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = new List<dynamic>();
            
            // Registry items
            foreach (var item in _viewModel.StartupRegistryItems)
            {
                csvData.Add(new
                {
                    Type = "Registry",
                    Name = item.Name,
                    Path = item.Path,
                    Source = item.Source,
                    Action = "",
                    Trigger = "",
                    Author = "",
                    LastRunTime = "",
                    NextRunTime = ""
                });
            }

            // Scheduled tasks
            foreach (var task in _viewModel.ScheduledTasks)
            {
                csvData.Add(new
                {
                    Type = "Scheduled Task",
                    Name = task.Name,
                    Path = task.Action,
                    Source = "Task Scheduler",
                    Action = task.Action,
                    Trigger = task.Trigger,
                    Author = task.Author,
                    LastRunTime = task.LastRunTime,
                    NextRunTime = task.NextRunTime
                });
            }

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = new
            {
                StartupRegistryItems = _viewModel.StartupRegistryItems.Select(item => new
                {
                    Name = item.Name,
                    Path = item.Path,
                    Source = item.Source
                }).ToList(),
                ScheduledTasks = _viewModel.ScheduledTasks.Select(task => new
                {
                    Name = task.Name,
                    Action = task.Action,
                    Trigger = task.Trigger,
                    Author = task.Author,
                    LastRunTime = task.LastRunTime,
                    NextRunTime = task.NextRunTime
                }).ToList()
            };

            ExportService.AutoExport(new[] { exportData }, GetModuleName());
        }

        public string GetModuleName()
        {
            return "StartupPrograms";
        }
    }

    public class StartupRegistryEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Source { get; set; }
    }

    public class ScheduledTaskEntryViewModel
    {
        public string Name { get; set; }
        public string Action { get; set; }
        public string Trigger { get; set; }
        public string Author { get; set; }
        public string LastRunTime { get; set; }
        public string NextRunTime { get; set; }
    }

    public class StartupProgramsViewModel
    {
        public ObservableCollection<StartupRegistryEntry> StartupRegistryItems { get; } = new ObservableCollection<StartupRegistryEntry>();
        public ObservableCollection<ScheduledTaskEntryViewModel> ScheduledTasks { get; } = new ObservableCollection<ScheduledTaskEntryViewModel>();

        public async Task LoadAllStartupItemsAsync()
        {
            var (registryItems, scheduledItems) = await Task.Run(() =>
            {
                var regItems = new List<StartupRegistryEntry>();
                var schedItems = new List<ScheduledTaskEntryViewModel>();

                ScanRegistryKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "Mevcut Kullanıcı (Registry)", regItems);
                ScanRegistryKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "Tüm Kullanıcılar (Registry)", regItems);
                ScanStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Startup (Kullanıcı)", regItems);
                ScanStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Startup (Tüm Kullanıcılar)", regItems);

                ScanScheduledTasks(schedItems);

                return (regItems.OrderBy(i => i.Name).ToList(), schedItems.OrderBy(i => i.Name).ToList());
            });

            StartupRegistryItems.Clear();
            foreach (var item in registryItems)
            {
                StartupRegistryItems.Add(item);
            }

            ScheduledTasks.Clear();
            foreach (var item in scheduledItems)
            {
                ScheduledTasks.Add(item);
            }
        }

        private void ScanRegistryKey(RegistryKey rootKey, string subKeyPath, string sourceName, List<StartupRegistryEntry> items)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(rootKey == Registry.LocalMachine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser, RegistryView.Default))
                using (var key = baseKey.OpenSubKey(subKeyPath))
                {
                    if (key == null) return;
                    foreach (var valueName in key.GetValueNames())
                    {
                        if (string.IsNullOrEmpty(valueName)) continue;
                        items.Add(new StartupRegistryEntry
                        {
                            Name = valueName,
                            Path = key.GetValue(valueName)?.ToString() ?? "",
                            Source = sourceName
                        });
                    }
                }
            }
            catch (Exception) { }
        }

        private void ScanStartupFolder(string folderPath, string sourceName, List<StartupRegistryEntry> items)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;
                foreach (var filePath in Directory.GetFiles(folderPath))
                {
                    items.Add(new StartupRegistryEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        Path = filePath,
                        Source = sourceName
                    });
                }
            }
            catch { }
        }

        private void ScanScheduledTasks(List<ScheduledTaskEntryViewModel> items)
        {
            try
            {
                using (TaskService ts = new TaskService())
                {
                    FindTasksInFolder(ts.RootFolder, items);
                }
            }
            catch (Exception) { }
        }

        // HATA ÇÖZÜMÜ 3: ITaskFolder yerine kütüphanenin kendi somut sınıfı olan TaskFolder kullanıyoruz.
        private void FindTasksInFolder(TaskFolder folder, List<ScheduledTaskEntryViewModel> items)
        {
            // HATA ÇÖZÜMÜ 4: "Task" çakışmasını önlemek için tam adını kullanıyoruz: Microsoft.Win32.TaskScheduler.Task
            foreach (Microsoft.Win32.TaskScheduler.Task task in folder.Tasks.Where(t => t.Enabled && t.Definition.Triggers.Any(trig => trig.TriggerType == TaskTriggerType.Logon || trig.TriggerType == TaskTriggerType.Boot)))
            {
                foreach (var action in task.Definition.Actions.OfType<ExecAction>())
                {
                    items.Add(new ScheduledTaskEntryViewModel
                    {
                        Name = task.Name,
                        Action = $"{action.Path} {action.Arguments}",
                        Author = task.Definition.RegistrationInfo.Author,
                        Trigger = FormatTriggerInfo(task.Definition.Triggers),
                        LastRunTime = task.LastRunTime.Year < 2000 ? "Hiç Çalışmadı" : task.LastRunTime.ToString("dd.MM.yyyy HH:mm"),
                        NextRunTime = task.NextRunTime.Year < 2000 ? "N/A" : task.NextRunTime.ToString("dd.MM.yyyy HH:mm")
                    });
                }
            }

            foreach (TaskFolder subFolder in folder.SubFolders)
            {
                FindTasksInFolder(subFolder, items);
            }
        }

        private string FormatTriggerInfo(TriggerCollection triggers)
        {
            var trigger = triggers.FirstOrDefault();
            if (trigger == null) return "Bilinmiyor";

            return trigger.TriggerType switch
            {
                TaskTriggerType.Logon => "Oturum açıldığında",
                TaskTriggerType.Boot => "Sistem başlangıcında",
                TaskTriggerType.Daily => "Günlük",
                TaskTriggerType.Weekly => "Haftalık",
                _ => trigger.ToString()
            };
        }
    }
}