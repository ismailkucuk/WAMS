using System;
using System.Collections.ObjectModel;
using System.Management;
using System.Windows;
using System.Windows.Controls;

namespace wam.Pages
{
    public partial class UsbMonitorPage : UserControl
    {
        private ObservableCollection<UsbEventEntry> logs = new();

        public UsbMonitorPage()
        {
            InitializeComponent();
            UsbLogList.ItemsSource = logs;
            StartUsbWatcher();
        }

        private void StartUsbWatcher()
        {
            var insertQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
            var removeQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");

            var insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    logs.Insert(0, new UsbEventEntry
                    {
                        Time = DateTime.Now.ToString("HH:mm:ss"),
                        Action = "🔌 Takıldı",
                        DeviceName = GetUsbDeviceDetails()
                    });
                });
            };
            insertWatcher.Start();

            var removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    logs.Insert(0, new UsbEventEntry
                    {
                        Time = DateTime.Now.ToString("HH:mm:ss"),
                        Action = "❌ Çıkarıldı",
                        DeviceName = "USB cihaz kaldırıldı"
                    });
                });
            };
            removeWatcher.Start();
        }

        private static string GetUsbDeviceDetails()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string model = device["Model"]?.ToString() ?? "Model yok";
                        string deviceID = device["DeviceID"]?.ToString() ?? "Yok";
                        string pnpID = device["PNPDeviceID"]?.ToString() ?? "Yok";

                        // Seri numarayı çekmeye çalış
                        string serial = "-";
                        try
                        {
                            using (var snSearcher = new ManagementObjectSearcher(
                                $"SELECT * FROM Win32_PhysicalMedia WHERE Tag = '{deviceID.Replace("\\", "\\\\")}'"))
                            {
                                foreach (ManagementObject media in snSearcher.Get())
                                {
                                    serial = media["SerialNumber"]?.ToString()?.Trim() ?? "-";
                                    break;
                                }
                            }
                        }
                        catch { }

                        // Boyut (GB)
                        long sizeBytes = device["Size"] != null ? Convert.ToInt64(device["Size"]) : 0;
                        string sizeGB = sizeBytes > 0 ? $"{Math.Round(sizeBytes / (1024.0 * 1024 * 1024), 2)} GB" : "-";

                        return $"{model} | Seri No: {serial} | Boyut: {sizeGB} | ID: {pnpID}";
                    }
                }
            }
            catch
            {
                // Hata durumunda sade bilgi dön
            }

            return "USB cihaz algılandı";
        }

        public class UsbEventEntry
        {
            public string Time { get; set; }
            public string Action { get; set; }
            public string DeviceName { get; set; }
        }

        private void DisableUsbPorts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExecuteCommand("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR /v Start /t REG_DWORD /d 4 /f");
                MessageBox.Show("Tüm USB depolama aygıtları devre dışı bırakıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void EnableUsbPorts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExecuteCommand("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\USBSTOR /v Start /t REG_DWORD /d 3 /f");
                MessageBox.Show("USB portları yeniden etkinleştirildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void ExecuteCommand(string command)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + command)
            {
                Verb = "runas", // YÖNETİCİ OLARAK ÇALIŞTIR
                CreateNoWindow = true,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }

    }
}
