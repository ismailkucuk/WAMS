using System;
using System.Windows;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace wam
{
    public partial class App : Application
    {
        private static Mutex _singleInstanceMutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            // Tek örnek kontrolü (VS altında iki simge oluşmasını engeller)
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "WAM_SINGLE_INSTANCE", out createdNew);
            if (!createdNew)
            {
                // Zaten çalışıyor; yeni örneği kapat
                Shutdown();
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                try { LogCrash(args.ExceptionObject as Exception, "AppDomain"); } catch { }
                MessageBox.Show($"Beklenmeyen bir hata oluştu. Uygulama devam etmeye çalışacak.\n\n{args.ExceptionObject}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            this.DispatcherUnhandledException += (s, args2) =>
            {
                try { LogCrash(args2.Exception, "Dispatcher"); } catch { }
                MessageBox.Show($"Bir hata oluştu ve yakalandı.\n\n{args2.Exception.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                args2.Handled = true; // Uygulamanın çökmesini engelle
            };

            TaskScheduler.UnobservedTaskException += (s, args3) =>
            {
                try { LogCrash(args3.Exception, "TaskScheduler"); } catch { }
                args3.SetObserved();
            };
            base.OnStartup(e);
        }

        private void LogCrash(Exception ex, string source)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAM", "Logs");
                Directory.CreateDirectory(folder);
                var file = Path.Combine(folder, $"crash_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{source}.log");
                File.WriteAllText(file, ex?.ToString() ?? "Unknown error");
            }
            catch { }
        }
    }
}
