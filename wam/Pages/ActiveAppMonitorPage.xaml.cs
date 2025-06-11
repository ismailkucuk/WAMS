using System;
using System.Collections.ObjectModel;
using System.Diagnostics; // Process ve PerformanceCounter için
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;    // System.Timers.Timer için
using System.Windows.Controls;
using System.Linq;      // OrderBy için

namespace wam.Pages
{
    public partial class ActiveAppMonitorPage : UserControl
    {
        private System.Timers.Timer _pollingTimer;
        // Artık sadece aktif pencere değil, tüm çalışan süreçleri izliyoruz.
        public ObservableCollection<ProcessInfo> RunningProcesses { get; set; } = new ObservableCollection<ProcessInfo>();

        public ActiveAppMonitorPage()
        {
            InitializeComponent();
            // DataGrid'in ItemsSource'unu yeni koleksiyona bağlayın.
            // XAML tarafında DataGrid'inizin Adı 'AppActivityGrid' ise bu satır doğru olacaktır.
            // Eğer ismini değiştirmek isterseniz XAML'de de güncellemelisiniz.
            AppActivityGrid.ItemsSource = RunningProcesses;

            _pollingTimer = new System.Timers.Timer(1000); // Her saniye kontrol et
            _pollingTimer.Elapsed += RefreshProcessList; // Yeni metodu bağla
            _pollingTimer.Start();

            // Sayfa kapatıldığında veya kullanıcı kontrolü kaldırıldığında timer'ı durdur.
            this.Unloaded += ActiveAppMonitorPage_Unloaded;

            // Uygulama ilk açıldığında bir kez süreç listesini yenile
            RefreshProcessList(null, null);
        }

        private void ActiveAppMonitorPage_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _pollingTimer.Stop();
            _pollingTimer.Dispose(); // Kaynakları serbest bırak
        }

        private void RefreshProcessList(object sender, ElapsedEventArgs e)
        {
            // UI güncellemeleri Dispatcher.Invoke içinde yapılmalıdır.
            Dispatcher.Invoke(() =>
            {
                RunningProcesses.Clear(); // Her yenilemede listeyi temizle

                Process[] processes = Process.GetProcesses();

                // Süreçleri isme göre sıralayalım
                foreach (Process p in processes.OrderBy(p => p.ProcessName))
                {
                    try
                    {
                        string windowTitle = "N/A";
                        if (p.MainWindowHandle != IntPtr.Zero) // Eğer ana penceresi varsa
                        {
                            var buffer = new StringBuilder(256);
                            if (GetWindowText(p.MainWindowHandle, buffer, buffer.Capacity) > 0)
                            {
                                windowTitle = buffer.ToString();
                            }
                        }

                        // Bellek kullanımı (Working Set)
                        long memoryBytes = p.WorkingSet64;
                        string memoryUsage = FormatBytes(memoryBytes);

                        // CPU kullanımı - Bu kısım daha karmaşık ve performans kritiktir.
                        // Her saniye her süreç için PerformanceCounter oluşturmak ve okumak maliyetli olabilir.
                        // Daha iyi bir yaklaşım, PerformanceCounter'ları bir kere oluşturup yeniden kullanmaktır.
                        // Basitlik adına burada "N/A" veya doğrudan TotalProcessorTime kullanılmıştır.
                        string cpuUsage = "N/A"; // Varsayılan değer
                        try
                        {
                            // Anlık CPU yüzdesini almak için PerformanceCounter gerekli.
                            // Örneğin:
                            // using (PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName, true))
                            // {
                            //     cpuCounter.NextValue(); // İlk okuma
                            //     // Gerçek bir uygulamada burada kısa bir bekleme (örn. 100ms) yapılır.
                            //     // Bu örnekte, anlık okuma yerine basit bir placeholder bırakılmıştır.
                            //     cpuUsage = cpuCounter.NextValue().ToString("F2") + "%";
                            // }

                            // Alternatif olarak, sürecin toplam CPU süresini gösterebiliriz (anlık yüzde değil).
                            // Bu da biraz yavaş olabilir.
                            TimeSpan cpuTime = p.TotalProcessorTime;
                            cpuUsage = $"{(int)cpuTime.TotalSeconds}s"; // Saniye cinsinden
                        }
                        catch (InvalidOperationException)
                        {
                            // Süreç kapanmış olabilir, PerformanceCounter hatası verebilir
                            cpuUsage = "Erişilemiyor";
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Erişim reddedildi
                            cpuUsage = "Erişim Reddedildi";
                        }


                        RunningProcesses.Add(new ProcessInfo
                        {
                            Name = p.ProcessName,
                            Id = p.Id,
                            WindowTitle = windowTitle,
                            MemoryUsage = memoryUsage,
                            CpuUsage = cpuUsage,
                            StartTime = p.StartTime
                        });
                    }
                    catch (InvalidOperationException)
                    {
                        // Süreç kapanmış veya başka bir nedenle geçerli değilse bu hata oluşabilir.
                        // Bu süreç atlanır.
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        // Bazı sistem süreçlerine erişim izni olmayabilir.
                        // Debug.WriteLine($"Error accessing process {p.ProcessName} (ID: {p.Id}): {ex.Message}");
                        RunningProcesses.Add(new ProcessInfo
                        {
                            Name = p.ProcessName,
                            Id = p.Id,
                            WindowTitle = "Erişim Reddedildi",
                            MemoryUsage = "N/A",
                            CpuUsage = "N/A",
                            StartTime = DateTime.MinValue // Geçerli bir başlangıç zamanı yok
                        });
                    }
                    catch (Exception ex)
                    {
                        // Diğer beklenmedik hatalar
                        // Debug.WriteLine($"An unexpected error occurred for process {p.ProcessName} (ID: {p.Id}): {ex.Message}");
                    }
                }
            });
        }

        // Byte değerini okunabilir formatlara çevirir (KB, MB, GB)
        private string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (dblSByte >= 1024 && i < Suffix.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return string.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        // --- P/Invoke Metotları (Windows API çağrıları) ---
        // Bu metotlar hala kullanışlıdır, özellikle bir sürecin ana pencere başlığını almak için.
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }

    // Yeni süreç bilgisi modeli
    public class ProcessInfo
    {
        public string Name { get; set; }        // Süreç Adı
        public int Id { get; set; }             // Süreç ID
        public string WindowTitle { get; set; } // Sürecin ana penceresinin başlığı
        public string MemoryUsage { get; set; } // Bellek kullanımı (örn: "25.5 MB")
        public string CpuUsage { get; set; }    // CPU kullanımı (örn: "5.2%", veya "30s")
        public DateTime StartTime { get; set; } // Sürecin başlangıç zamanı

        // UI'da kolayca gösterebilmek için formatlı başlangıç zamanı
        public string StartTimeFormatted => StartTime.ToString("HH:mm:ss");
    }
}