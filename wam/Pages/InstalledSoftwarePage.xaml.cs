using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using wam.Services;

namespace wam.Pages
{
    public partial class InstalledSoftwarePage : UserControl, ILoadablePage
    {
        private InstalledSoftwareViewModel _viewModel;
        public InstalledSoftwarePage()
        {
            InitializeComponent();
            _viewModel = new InstalledSoftwareViewModel();
            this.DataContext = _viewModel;
            
            // Export control'ü bu sayfaya bağla
            ExportControl.TargetPage = this;
        }

        public async Task LoadDataAsync()
        {
            await _viewModel.LoadSoftwareAsync();
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = _viewModel.SoftwareList.Select(s => new
            {
                Name = s.Name,
                Publisher = s.Publisher,
                Version = s.Version,
                InstallDate = s.InstallDate,
                EstimatedSizeKB = s.EstimatedSizeKB,
                FormattedSize = s.FormattedSize,
                CanUninstall = s.CanUninstall,
                UninstallString = s.UninstallString
            }).ToList();

            ExportService.ExportToJson(exportData, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = _viewModel.SoftwareList.Select(s => new
            {
                Name = s.Name,
                Publisher = s.Publisher,
                Version = s.Version,
                InstallDate = s.InstallDate,
                SizeKB = s.EstimatedSizeKB,
                FormattedSize = s.FormattedSize,
                CanUninstall = s.CanUninstall,
                SortField = _viewModel.SortField,
                SortDescending = _viewModel.SortDescending
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = _viewModel.SoftwareList.Select(s => new
            {
                Name = s.Name,
                Publisher = s.Publisher,
                Version = s.Version,
                InstallDate = s.InstallDate,
                EstimatedSizeKB = s.EstimatedSizeKB,
                FormattedSize = s.FormattedSize,
                CanUninstall = s.CanUninstall
            }).ToList();

            ExportService.AutoExport(exportData, GetModuleName());
        }

        public string GetModuleName()
        {
            return "InstalledSoftware";
        }

        private void Sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && _viewModel.SoftwareList.Any())
                _viewModel.ApplySorting();
        }

        private void SortDirection_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.SoftwareList.Any())
                _viewModel.ApplySorting();
        }
    }

    public class SoftwareInfoViewModel
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string Version { get; set; }
        public string InstallDate { get; set; }
        public long EstimatedSizeKB { get; set; }
        public string UninstallString { get; set; }

        public string FormattedSize => EstimatedSizeKB > 0 ? $"{Math.Ceiling(EstimatedSizeKB / 1024.0)} MB" : "-";
        public bool CanUninstall => !string.IsNullOrEmpty(UninstallString);
    }

    public class InstalledSoftwareViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SoftwareInfoViewModel> SoftwareList { get; } = new ObservableCollection<SoftwareInfoViewModel>();
        private List<SoftwareInfoViewModel> _fullSoftwareList = new List<SoftwareInfoViewModel>();

        private string _sortField = "Name";
        public string SortField
        {
            get => _sortField;
            set { _sortField = value; OnPropertyChanged(); }
        }

        private bool _sortDescending;
        public bool SortDescending
        {
            get => _sortDescending;
            set { _sortDescending = value; OnPropertyChanged(); }
        }

        public ICommand UninstallCommand { get; }

        public InstalledSoftwareViewModel()
        {
            UninstallCommand = new RelayCommand(UninstallProgram);
        }

        public async Task LoadSoftwareAsync()
        {
            var softwareItems = await Task.Run(() =>
            {
                var allSoftware = new List<SoftwareInfoViewModel>();
                ScanRegistryForSoftware(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", allSoftware);
                ScanRegistryForSoftware(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", allSoftware);

                return allSoftware
                    .Where(s => !string.IsNullOrEmpty(s.Name))
                    .GroupBy(s => s.Name)
                    .Select(g => g.First())
                    .ToList();
            });

            _fullSoftwareList = softwareItems;
            ApplySorting();
        }

        private void ScanRegistryForSoftware(string keyPath, List<SoftwareInfoViewModel> softwareList)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) return;
                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                        {
                            if (subkey == null) continue;
                            var displayName = subkey.GetValue("DisplayName") as string;
                            var systemComponent = subkey.GetValue("SystemComponent") as int?;

                            if (string.IsNullOrEmpty(displayName) || systemComponent == 1) continue;

                            softwareList.Add(new SoftwareInfoViewModel
                            {
                                Name = displayName,
                                Publisher = subkey.GetValue("Publisher") as string ?? "-",
                                Version = subkey.GetValue("DisplayVersion") as string ?? "-",
                                InstallDate = subkey.GetValue("InstallDate") as string ?? "-",
                                EstimatedSizeKB = Convert.ToInt64(subkey.GetValue("EstimatedSize") ?? 0),
                                UninstallString = subkey.GetValue("UninstallString") as string
                            });
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public void ApplySorting()
        {
            IEnumerable<SoftwareInfoViewModel> sortedList = _fullSoftwareList;
            switch (SortField)
            {
                case "Publisher":
                    sortedList = SortDescending ? sortedList.OrderByDescending(s => s.Publisher) : sortedList.OrderBy(s => s.Publisher);
                    break;
                case "Size":
                    sortedList = SortDescending ? sortedList.OrderByDescending(s => s.EstimatedSizeKB) : sortedList.OrderBy(s => s.EstimatedSizeKB);
                    break;
                case "Name":
                default:
                    sortedList = SortDescending ? sortedList.OrderByDescending(s => s.Name) : sortedList.OrderBy(s => s.Name);
                    break;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                SoftwareList.Clear();
                foreach (var item in sortedList)
                {
                    SoftwareList.Add(item);
                }
            });
        }

        private void UninstallProgram(object parameter)
        {
            if (parameter is SoftwareInfoViewModel software && software.CanUninstall)
            {
                var result = MessageBox.Show($"'{software.Name}' yazılımını kaldırmak istediğinize emin misiniz?\n\nBu işlem, programın kendi kaldırma aracını çalıştıracaktır.", "Kaldırma Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;

                try
                {
                    string uninstallCommand = software.UninstallString.Trim('"', '\'');
                    string command;
                    string args = "";

                    if (uninstallCommand.ToLower().Contains("msiexec.exe"))
                    {
                        command = "msiexec.exe";
                        args = uninstallCommand.Substring(uninstallCommand.ToLower().IndexOf("/")).Trim();
                    }
                    else if (uninstallCommand.Contains(".exe"))
                    {
                        int exeIndex = uninstallCommand.ToLower().IndexOf(".exe");
                        command = uninstallCommand.Substring(0, exeIndex + 4);
                        command = command.Trim('"', '\'');
                        args = uninstallCommand.Length > exeIndex + 4 ? uninstallCommand.Substring(exeIndex + 5) : "";
                    }
                    else
                    {
                        command = uninstallCommand;
                    }

                    Process.Start(new ProcessStartInfo(command, args) { UseShellExecute = true, Verb = "runas" });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Program kaldırıcısı çalıştırılamadı (Yönetici izni gerekebilir):\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) { _execute = execute; }
        public bool CanExecute(object parameter) => true;
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public void Execute(object parameter) => _execute(parameter);
    }
}