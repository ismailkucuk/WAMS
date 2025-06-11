using System;
using System.Windows;

namespace wam
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                MessageBox.Show($"Beklenmeyen bir hata oluştu:\n{args.ExceptionObject}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            base.OnStartup(e);
        }
    }
}
