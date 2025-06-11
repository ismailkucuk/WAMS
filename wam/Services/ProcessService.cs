using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace wam.Services
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public int ParentId { get; set; }
        public string ParentName { get; set; }
    }

    public class ProcessService
    {
        public static List<ProcessInfo> GetProcesses()
        {
            List<ProcessInfo> list = new List<ProcessInfo>();
            Dictionary<int, string> pidNameMap = new Dictionary<int, string>();

            try
            {
                Process[] all = Process.GetProcesses();

                // Tüm PID ve adları eşleştir
                foreach (var proc in all)
                {
                    try
                    {
                        pidNameMap[proc.Id] = proc.ProcessName;
                    }
                    catch { }
                }

                // Her işlem için detaylı bilgi
                foreach (var proc in all)
                {
                    try
                    {
                        int parentId = GetParentProcessId(proc.Id);
                        pidNameMap.TryGetValue(parentId, out string parentName);

                        var pi = new ProcessInfo
                        {
                            Id = proc.Id,
                            Name = proc.ProcessName,
                            StartTime = proc.StartTime,
                            ParentId = parentId,
                            ParentName = parentName ?? "Bilinmiyor"
                        };

                        list.Add(pi);
                    }
                    catch { /* Bazı sistem process'leri erişilemez olabilir */ }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Process error: " + ex.Message);
            }

            return list;
        }

        private static int GetParentProcessId(int pid)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE ProcessId = " + pid))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return Convert.ToInt32(obj["ParentProcessId"]);
                    }
                }
            }
            catch { }

            return 0;
        }
    }
}
