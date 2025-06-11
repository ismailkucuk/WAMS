using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using wam.Helpers;

namespace wam.Services
{
    public class ConnectionEntry
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string LocalAddress { get; set; }
        public int LocalPort { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePort { get; set; }
        public string State { get; set; }
        public string Protocol { get; set; }
        public string RiskLabel { get; set; }
        public bool IsBlocked { get; set; }
        public string RemoteDomain { get; set; }


    }

    public class AdvancedNetworkService
    {
        private static readonly HashSet<int> CriticalPorts = new HashSet<int> { 21, 22, 23, 25, 53, 80, 139, 443, 445, 3389 };

        public static List<ConnectionEntry> GetAllConnections(bool onlyListening = false, bool onlyCritical = false)
        {
            List<ConnectionEntry> list = new List<ConnectionEntry>();
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = props.GetActiveTcpConnections();

            foreach (var conn in tcpConnections)
            {
                if (conn.RemoteEndPoint == null)
                    continue; // Hatalı bağlantı varsa geç

                int localPort = conn.LocalEndPoint.Port;

                if (onlyListening && conn.State != TcpState.Listen)
                    continue;

                if (onlyCritical && !CriticalPorts.Contains(localPort))
                    continue;

                int pid = GetPidFromPort(localPort);
                string pname = "Bilinmiyor";

                try
                {
                    pname = Process.GetProcessById(pid).ProcessName;
                }
                catch { }

                string risk = "Normal";
                if (CriticalPorts.Contains(localPort))
                    risk = "⚠ Kritik Port";

                bool isBlocked = FirewallRuleExists($"WAM_Block_{localPort}");
                var domain = DnsResolver.Resolve(conn.RemoteEndPoint?.Address.ToString() ?? "-");

                list.Add(new ConnectionEntry
                {
                    ProcessId = pid,
                    ProcessName = pname,
                    LocalAddress = conn.LocalEndPoint.Address.ToString(),
                    LocalPort = conn.LocalEndPoint.Port,
                    RemoteAddress = conn.RemoteEndPoint?.Address.ToString() ?? "-",
                    RemotePort = conn.RemoteEndPoint?.Port ?? 0,
                    Protocol = "TCP",
                    State = conn.State.ToString(),
                    RemoteDomain = domain,
                    RiskLabel = risk,
                    IsBlocked = isBlocked
                });
            }

            return list;
        }


        private static int GetPidFromPort(int port)
        {
            try
            {
                var psi = new ProcessStartInfo("netstat", "-ano")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        var line = proc.StandardOutput.ReadLine();
                        if (line.Contains($":{port}"))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                                return pid;
                        }
                    }
                }
            }
            catch { }

            return 0;
        }

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
    }
}
