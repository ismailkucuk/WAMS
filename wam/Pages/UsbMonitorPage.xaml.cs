using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using wam.Services;

namespace wam.Pages
{
    public partial class UsbMonitorPage : UserControl, ILoadablePage
    {
        private readonly UsbMonitorViewModel _viewModel;

        public UsbMonitorPage()
        {
            InitializeComponent();
            _viewModel = new UsbMonitorViewModel();
            this.DataContext = _viewModel;

            this.Unloaded += (s, e) => _viewModel.Dispose();
        }

        public async Task LoadDataAsync()
        {
            // USB monitor zaten constructor'da başlıyor; ek bir yükleme yok
            await Task.CompletedTask;
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            var exportData = new
            {
                UsbPortStatus = new
                {
                    ArePortsEnabled = _viewModel.ArePortsEnabled,
                    ArePortsDisabled = _viewModel.ArePortsDisabled
                },
                UsbEvents = _viewModel.Logs.Select(log => new
                {
                    Time = log.Time,
                    Action = log.Action,
                    DeviceName = log.DeviceName
                }).ToList()
            };

            ExportService.ExportToJson(new[] { exportData }, GetModuleName());
        }

        public void ExportToCsv()
        {
            var csvData = _viewModel.Logs.Select(log => new
            {
                Time = log.Time,
                Action = log.Action,
                DeviceName = log.DeviceName,
                PortsEnabled = _viewModel.ArePortsEnabled
            }).ToList();

            ExportService.ExportToCsv(csvData, GetModuleName());
        }

        public void AutoExport()
        {
            var exportData = new
            {
                UsbPortStatus = new
                {
                    ArePortsEnabled = _viewModel.ArePortsEnabled,
                    ArePortsDisabled = _viewModel.ArePortsDisabled
                },
                UsbEvents = _viewModel.Logs.Select(log => new
                {
                    Time = log.Time,
                    Action = log.Action,
                    DeviceName = log.DeviceName
                }).ToList()
            };

            ExportService.AutoExport(new[] { exportData }, GetModuleName());
        }

        public string GetModuleName()
        {
            return "UsbMonitor";
        }

        private void EnableUsbPorts_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.EnablePorts();
        }

        private void DisableUsbPorts_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DisablePorts();
        }
    }

    public class UsbEventEntry
    {
        public string Time { get; set; }
        public string Action { get; set; }
        public string DeviceName { get; set; }
    }

    // YENİ: Cihazın hem donanım kimliğini hem de detayını tutan yardımcı sınıf
    public class UsbDeviceInfo
    {
        public string PnpDeviceId { get; set; }
        public string FormattedDetails { get; set; }
    }

    public class UsbMonitorViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool _arePortsEnabled;
        private readonly ManagementEventWatcher _insertWatcher;
        private readonly ManagementEventWatcher _removeWatcher;

        // YENİ: O an bağlı olan cihazları takip etmek için bir liste
        private readonly List<UsbDeviceInfo> _currentlyConnectedDevices = new List<UsbDeviceInfo>();

        public ObservableCollection<UsbEventEntry> Logs { get; } = new ObservableCollection<UsbEventEntry>();
        public bool ArePortsEnabled
        {
            get => _arePortsEnabled;
            set
            {
                if (_arePortsEnabled != value)
                {
                    _arePortsEnabled = value;
                    OnPropertyChanged(nameof(ArePortsEnabled));
                    OnPropertyChanged(nameof(ArePortsDisabled));
                }
            }
        }
        public bool ArePortsDisabled => !ArePortsEnabled;

        public UsbMonitorViewModel()
        {
            CheckInitialUsbState();

            var insertQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
            _insertWatcher = new ManagementEventWatcher(insertQuery);
            _insertWatcher.EventArrived += OnUsbDeviceInserted;
            _insertWatcher.Start();

            var removeQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
            _removeWatcher = new ManagementEventWatcher(removeQuery);
            _removeWatcher.EventArrived += OnUsbDeviceRemoved;
            _removeWatcher.Start();

            AddLog("Takip Başladı", "USB olayları dinleniyor...");
        }

        // DEĞİŞİKLİK: USB takıldığında çalışan metot artık daha akıllı
        private void OnUsbDeviceInserted(object sender, EventArrivedEventArgs e)
        {
            System.Threading.Thread.Sleep(1500); // Sürücü harfinin atanması için bekleme

            // Sisteme yeni takılan ve daha önce loglamadığımız cihazları bul
            var newDevices = ScanForNewUsbDevices();

            foreach (var newDevice in newDevices)
            {
                // Aktif cihaz listemize ekle
                _currentlyConnectedDevices.Add(newDevice);
                // Ve loga kaydını düş
                AddLog("🔌 Takıldı", newDevice.FormattedDetails);
            }
        }

        // DEĞİŞİKLİK: USB çıkarıldığında çalışan metot artık daha akıllı
        private void OnUsbDeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            System.Threading.Thread.Sleep(500); // WMI'ın güncellenmesi için kısa bekleme

            // Şu an sistemde olan USB'lerin donanım kimliklerini al
            var currentSystemPnpDeviceIds = GetCurrentUsbPnpDeviceIds();

            // Bizim listemizde olup da artık sistemde olmayanları bul (yani çıkarılanları)
            var removedDevices = _currentlyConnectedDevices
                .Where(d => !currentSystemPnpDeviceIds.Contains(d.PnpDeviceId))
                .ToList();

            foreach (var removedDevice in removedDevices)
            {
                // Çıkarılan cihazın sakladığımız bilgisini loga yaz
                AddLog("❌ Çıkarıldı", removedDevice.FormattedDetails);
                // Ve kendi aktif listemizden de sil
                _currentlyConnectedDevices.Remove(removedDevice);
            }
        }

        // YENİ: Henüz loglanmamış yeni cihazları tarayan metot
        private List<UsbDeviceInfo> ScanForNewUsbDevices()
        {
            var newDevicesFound = new List<UsbDeviceInfo>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string pnpDeviceId = device["PNPDeviceID"]?.ToString();
                        // Eğer bu cihazı daha önce aktif listemize eklemediysek, bu yeni bir cihazdır.
                        if (!string.IsNullOrEmpty(pnpDeviceId) && !_currentlyConnectedDevices.Any(d => d.PnpDeviceId == pnpDeviceId))
                        {
                            var detailsBuilder = new StringBuilder();
                            string model = device["Model"]?.ToString() ?? "Bilinmeyen Model";
                            string manufacturer = device["Manufacturer"]?.ToString() ?? "Bilinmeyen Üretici";
                            string logicalDiskInfo = GetLogicalDiskInfoFor(device);

                            detailsBuilder.AppendLine($"{model} ({manufacturer})");
                            if (!string.IsNullOrEmpty(logicalDiskInfo))
                            {
                                detailsBuilder.AppendLine($"  └ Sürücü Bilgileri: {logicalDiskInfo}");
                            }
                            detailsBuilder.Append($"  └ Donanım Kimliği: {pnpDeviceId}");

                            newDevicesFound.Add(new UsbDeviceInfo
                            {
                                PnpDeviceId = pnpDeviceId,
                                FormattedDetails = detailsBuilder.ToString()
                            });
                        }
                    }
                }
            }
            catch { }
            return newDevicesFound;
        }

        // YENİ: Sadece o an bağlı olan cihazların PnpDeviceID listesini döndüren yardımcı metot
        private HashSet<string> GetCurrentUsbPnpDeviceIds()
        {
            var ids = new HashSet<string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT PNPDeviceID FROM Win32_DiskDrive WHERE InterfaceType='USB'"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string pnpDeviceId = device["PNPDeviceID"]?.ToString();
                        if (!string.IsNullOrEmpty(pnpDeviceId))
                        {
                            ids.Add(pnpDeviceId);
                        }
                    }
                }
            }
            catch { }
            return ids;
        }

        public void EnablePorts()
        {
            try
            {
                ExecuteCommand("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR /v Start /t REG_DWORD /d 3 /f");
                MessageBox.Show("USB portları yeniden etkinleştirildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                AddLog("Portlar Açıldı", "Kullanıcı tarafından yapıldı");
                ArePortsEnabled = true;
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        public void DisablePorts()
        {
            try
            {
                ExecuteCommand("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR /v Start /t REG_DWORD /d 4 /f");
                MessageBox.Show("Tüm USB depolama aygıtları devre dışı bırakıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Warning);
                AddLog("Portlar Kapatıldı", "Kullanıcı tarafından yapıldı");
                ArePortsEnabled = false;
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        private void AddLog(string action, string deviceName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, new UsbEventEntry
                {
                    Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                    Action = action,
                    DeviceName = deviceName
                });
            });
        }

        private void CheckInitialUsbState()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USBSTOR"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("Start");
                        ArePortsEnabled = (value != null && (int)value == 3);
                    }
                }
            }
            catch { ArePortsEnabled = true; }
        }

        private void ExecuteCommand(string command)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + command)
            {
                Verb = "runas",
                CreateNoWindow = true,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }

        private string GetLogicalDiskInfoFor(ManagementObject diskDrive)
        {
            try
            {
                var logicalDiskDetails = new List<string>();
                foreach (ManagementObject partition in diskDrive.GetRelated("Win32_DiskPartition"))
                {
                    foreach (ManagementObject logicalDisk in partition.GetRelated("Win32_LogicalDisk"))
                    {
                        string deviceId = logicalDisk["DeviceID"]?.ToString(); // E:
                        string volumeName = logicalDisk["VolumeName"]?.ToString() ?? "İsimsiz";
                        string fileSystem = logicalDisk["FileSystem"]?.ToString() ?? "?";
                        long totalSize = logicalDisk["Size"] != null ? Convert.ToInt64(logicalDisk["Size"]) : 0;
                        string sizeGb = totalSize > 0 ? $"{Math.Round(totalSize / (1024.0 * 1024 * 1024), 2)} GB" : "-";

                        logicalDiskDetails.Add($"{volumeName} ({deviceId}) - {sizeGb} [{fileSystem}]");
                    }
                }
                return string.Join(", ", logicalDiskDetails);
            }
            catch { return "Alınamadı"; }
        }


        public void Dispose()
        {
            _insertWatcher?.Stop();
            _insertWatcher?.Dispose();
            _removeWatcher?.Stop();
            _removeWatcher?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}