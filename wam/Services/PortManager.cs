using System;
using System.Diagnostics;
using System.IO;

namespace wam.Services
{
    public class PortManager
    {
        private static bool FirewallRuleExists(string ruleName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    return output.Contains(ruleName);
                }
            }
            catch
            {
                return false;
            }
        }

        public static void BlockPort(int port)
        {
            string ruleName = $"WAM_Block_{port}";

            if (FirewallRuleExists(ruleName))
            {
                System.Windows.MessageBox.Show($"Port {port} zaten engellenmiş.", "Bilgi");
                return;
            }

            string command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir=in action=block protocol=TCP localport={port}";
            RunCommandAsAdmin(command);

            System.Windows.MessageBox.Show($"Port {port} başarıyla engellendi.\nYeni gelen bağlantılar engellenecek.", "Başarılı");
        }

        public static void UnblockPort(int port)
        {
            string ruleName = $"WAM_Block_{port}";
            string command = $"netsh advfirewall firewall delete rule name=\"{ruleName}\"";
            RunCommandAsAdmin(command);
            System.Windows.MessageBox.Show($"Port {port} için engelleme kaldırıldı.", "Bilgi");
        }

        private static void RunCommandAsAdmin(string cmd)
        {
            try
            {
                // Geçici .bat dosyası oluştur
                string tempPath = Path.Combine(Path.GetTempPath(), "wam_firewall_cmd.bat");
                File.WriteAllText(tempPath, cmd);

                // .bat dosyasını yönetici olarak çalıştır
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("İşlem iptal edildi veya başarısız oldu:\n" + ex.Message);
            }
        }
    }
}
