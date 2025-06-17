using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Windows.Controls;
using System.IO;

namespace wam.Pages
{
    public partial class SystemInfoPage : UserControl
    {
        public SystemInfoPage()
        {
            InitializeComponent();
            LoadSystemInfo();
        }

        private void LoadSystemInfo()
        {
            // Bu kısımda bir değişiklik yok, olduğu gibi kalıyor.
            UserNameText.Text = Environment.UserName;
            MachineNameText.Text = Environment.MachineName;
            IpAddressText.Text = GetLocalIPAddress();
            OsVersionText.Text = $"{Environment.OSVersion}";
            ArchitectureText.Text = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

            var (total, available) = GetRamInfo();
            TotalRamText.Text = total;
            AvailableRamText.Text = available;

            DomainText.Text = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            BiosText.Text = GetBiosVersion();

            // DEĞİŞİKLİK 3: Veriyi eski DataGrid yerine yeni ItemsControl'e bağlıyoruz.
            // XAML'de DataGrid'i kaldırıp yerine ItemsControl eklediğimiz için bu satırı güncelledik.
            DiskUsageItemsControl.ItemsSource = GetDiskInfo();
        }

        // Bu metodlarda bir değişiklik yok, olduğu gibi kalıyorlar.
        #region Helper Methods
        private string GetLocalIPAddress()
        {
            try
            {
                string localIP = Dns.GetHostEntry(Dns.GetHostName())
                                     .AddressList
                                     .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                     ?.ToString();
                return localIP ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private (string Total, string Available) GetRamInfo()
        {
            try
            {
                using var mos = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in mos.Get())
                {
                    double totalKb = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                    double freeKb = Convert.ToDouble(obj["FreePhysicalMemory"]);

                    string total = $"{(totalKb / 1024 / 1024):0.00} GB";
                    string free = $"{(freeKb / 1024 / 1024):0.00} GB";

                    return (total, free);
                }
            }
            catch { }

            return ("-", "-");
        }

        private string GetBiosVersion()
        {
            try
            {
                using var mos = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion FROM Win32_BIOS");
                foreach (ManagementObject obj in mos.Get())
                {
                    return obj["SMBIOSBIOSVersion"]?.ToString() ?? "-";
                }
            }
            catch { }

            return "-";
        }
        #endregion

        // DEĞİŞİKLİK 2: GetDiskInfo metodu, yeni ViewModel'i kullanacak şekilde güncellendi.
        // Artık string listesi değil, içinde hesaplama yapabilen bir nesne listesi döndürüyor.
        private List<DiskInfoViewModel> GetDiskInfo()
        {
            var list = new List<DiskInfoViewModel>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        // String formatlama yapmak yerine, ham byte verilerini direkt olarak
                        // yeni ViewModel'imize aktarıyoruz.
                        list.Add(new DiskInfoViewModel
                        {
                            Name = drive.Name,
                            TotalBytes = drive.TotalSize,
                            FreeBytes = drive.AvailableFreeSpace
                        });
                    }
                }
                catch { } // Özellikle CD-ROM gibi sürücülerde hata verebilir, yutmak mantıklı.
            }

            return list;
        }

        // DEĞİŞİKLİK 1: Eski DiskInfo sınıfı, ProgressBar'ı destekleyen yeni ve daha yetenekli
        // DiskInfoViewModel sınıfı ile değiştirildi.
        public class DiskInfoViewModel
        {
            // Temel veriler (byte olarak)
            public long TotalBytes { get; set; }
            public long FreeBytes { get; set; }

            // XAML'de kullanılacak hesaplanmış veriler
            public string Name { get; set; } // "C:\", "D:\"
            public long UsedBytes => TotalBytes - FreeBytes;

            // ProgressBar'a bağlanacak değer (% olarak)
            public double PercentageUsed => (TotalBytes > 0) ? ((double)UsedBytes / TotalBytes) * 100 : 0;

            // Ekranda gösterilecek formatlanmış metinler
            public string DisplayTotalSize => $"{TotalBytes / (1024.0 * 1024 * 1024):F2} GB";
            public string DisplayFreeSpace => $"{FreeBytes / (1024.0 * 1024 * 1024):F2} GB";
            public string UsageText => $"{DisplayFreeSpace} boş / {DisplayTotalSize} toplam";
        }
    }
}