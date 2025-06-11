using System;
using System.Collections.Concurrent;
using System.Net;

namespace wam.Helpers
{
    public static class DnsResolver
    {
        private static ConcurrentDictionary<string, string> cache = new();

        public static string Resolve(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || ip == "-")
                return "-";

            if (cache.ContainsKey(ip))
                return cache[ip];

            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ip);
                string domain = entry.HostName;
                cache[ip] = domain;
                return domain;
            }
            catch
            {
                cache[ip] = "-";
                return "-";
            }
        }
    }
}
