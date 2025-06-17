using System.Diagnostics;
using System.Windows;

namespace wam.Pages
{
    public partial class EventDetailWindow : Window
    {
        private readonly EventLogEntryViewModel _viewModel;

        public EventDetailWindow(EventLogEntryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        private void SearchOnline_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.Source)) return;
            string url = $"https://www.ultimatewindowssecurity.com/securitylog/encyclopedia/event.aspx?eventid={_viewModel.EventID}";

            // Güvenlik için UseShellExecute = true kullanmak önemlidir.
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_viewModel.Message);
            MessageBox.Show("Olay mesajının tamamı panoya kopyalandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}