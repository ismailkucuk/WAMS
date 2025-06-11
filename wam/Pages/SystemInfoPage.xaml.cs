using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
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
            UserNameText.Text = Environment.UserName;
            MachineNameText.Text = Environment.MachineName;
            IpAddressText.Text = GetLocalIPAddress();
            OsVersionText.Text = $"{Environment.OSVersion}";
            ArchitectureText.Text = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

            var (total, available) = GetRamInfo();
            TotalRamText.Text = total;
            AvailableRamText.Text = available;

            DomainText.Text = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            BiosText.Text = GetBiosVersion();

            DiskInfoGrid.ItemsSource = GetDiskInfo();
        }

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

        private List<DiskInfo> GetDiskInfo()
        {
            var list = new List<DiskInfo>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        list.Add(new DiskInfo
                        {
                            Name = drive.Name,
                            TotalSize = $"{(drive.TotalSize / 1024 / 1024 / 1024.0):0.00} GB",
                            FreeSpace = $"{(drive.TotalFreeSpace / 1024 / 1024 / 1024.0):0.00} GB"
                        });
                    }
                }
                catch { }
            }

            return list;
        }

        public class DiskInfo
        {
            public string Name { get; set; }
            public string TotalSize { get; set; }
            public string FreeSpace { get; set; }
        }
    }
}
