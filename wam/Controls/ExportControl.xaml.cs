using System.Windows;
using System.Windows.Controls;
using wam.Pages;

namespace wam.Controls
{
    public partial class ExportControl : UserControl
    {
        public ILoadablePage TargetPage { get; set; }

        public ExportControl()
        {
            InitializeComponent();
        }

        private void JsonExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TargetPage?.ExportToJson();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"JSON export hatası:\n{ex.Message}", 
                    "Hata", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void CsvExportButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                TargetPage?.ExportToCsv();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"CSV export hatası: {ex.Message}",
                    "Hata",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
} 