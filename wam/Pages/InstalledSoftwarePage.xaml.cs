using System;
using System.Collections.Generic;
using System.Management;
using System.Windows;
using System.Windows.Controls;

namespace wam.Pages
{
    public partial class InstalledSoftwarePage : UserControl
    {
        public InstalledSoftwarePage()
        {
            InitializeComponent();
            LoadInstalledSoftware();
        }

        private void FetchSoftwareList_Click(object sender, RoutedEventArgs e)
        {
            LoadInstalledSoftware();
        }

        private void LoadInstalledSoftware()
        {
            var softwareList = new List<SoftwareInfo>();

            try
            {
                string query = "SELECT * FROM Win32_Product";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    softwareList.Add(new SoftwareInfo
                    {
                        Name = obj["Name"]?.ToString() ?? "Bilinmiyor",
                        Version = obj["Version"]?.ToString() ?? "-",
                        Publisher = obj["Vendor"]?.ToString() ?? "-",
                        InstallDate = ParseInstallDate(obj["InstallDate"]?.ToString())
                    });
                }

                SoftwareDataGrid.ItemsSource = softwareList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler alınamadı: {ex.Message}");
            }
        }


        private string ParseInstallDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate) || rawDate.Length < 8)
                return "-";

            try
            {
                return DateTime.ParseExact(rawDate.Substring(0, 8), "yyyyMMdd", null).ToShortDateString();
            }
            catch
            {
                return "-";
            }
        }

        public class SoftwareInfo
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Publisher { get; set; }
            public string InstallDate { get; set; }
        }
    }
}
