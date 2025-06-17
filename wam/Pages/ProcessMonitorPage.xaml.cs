using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using wam.Services;

namespace wam.Pages
{
    // DEĞİŞİKLİK: Sınıfın, oluşturduğumuz ILoadablePage arayüzünü uyguladığını belirtiyoruz.
    public partial class ProcessMonitorPage : UserControl, ILoadablePage
    {
        private List<ProcessInfo> _processData;

        // Constructor'ın sade hali doğru.
        public ProcessMonitorPage()
        {
            InitializeComponent();
            
            // Export control'ü bu sayfaya bağla
            ExportControl.TargetPage = this;
        }

        // Bu metodun yapısı da yeni sistemimiz için mükemmel.
        public async Task LoadDataAsync()
        {
            // Ağır işlemi arka plan thread'ine alıyoruz.
            _processData = await Task.Run(() => ProcessService.GetProcesses());

            // Veri hazır olduğunda, listeyi arayüze bağlıyoruz.
            ProcessListView.ItemsSource = _processData;
        }

        // ILoadablePage export metodları
        public void ExportToJson()
        {
            if (_processData != null)
            {
                ExportService.ExportToJson(_processData, GetModuleName());
            }
        }

        public void ExportToCsv()
        {
            if (_processData != null)
            {
                ExportService.ExportToCsv(_processData, GetModuleName());
            }
        }

        public void AutoExport()
        {
            if (_processData != null)
            {
                ExportService.AutoExport(_processData, GetModuleName());
            }
        }

        public string GetModuleName()
        {
            return "ProcessMonitor";
        }
    }
}