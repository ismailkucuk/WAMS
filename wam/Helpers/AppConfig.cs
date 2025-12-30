using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace wam.Helpers
{
    /// <summary>
    /// Configuration model for application settings (snow effect, messages, etc.)
    /// Designed to be easily switchable from local file to remote URL.
    /// </summary>
    public class AppConfig
    {
        [JsonPropertyName("snow_effect")]
        public bool SnowEffect { get; set; } = false;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("snowflake_count")]
        public int SnowflakeCount { get; set; } = 50;

        [JsonPropertyName("min_speed")]
        public double MinSpeed { get; set; } = 1.0;

        [JsonPropertyName("max_speed")]
        public double MaxSpeed { get; set; } = 4.0;

        [JsonPropertyName("min_size")]
        public double MinSize { get; set; } = 3.0;

        [JsonPropertyName("max_size")]
        public double MaxSize { get; set; } = 8.0;
    }

    /// <summary>
    /// Service class to load configuration from local file or remote URL.
    /// Implements remote-first loading: loads local config immediately for fast startup,
    /// then fetches remote config in background and overrides if successful.
    /// </summary>
    public static class ConfigService
    {
        private static readonly string LocalConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "local_config.json");

        private const string RemoteConfigUrl =
            "https://raw.githubusercontent.com/ismailkucuk/WAMS/master/wam/local_config.json";

        private const int RemoteTimeoutSeconds = 3;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(RemoteTimeoutSeconds)
        };

        private static AppConfig _cachedConfig = null;

        /// <summary>
        /// Event fired when remote config is successfully loaded and differs from local.
        /// UI can subscribe to this to refresh settings dynamically.
        /// </summary>
        public static event Action<AppConfig> RemoteConfigLoaded;

        /// <summary>
        /// Loads configuration from local JSON file.
        /// Returns default config if file doesn't exist or parsing fails.
        /// </summary>
        public static AppConfig LoadConfig()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            try
            {
                if (!File.Exists(LocalConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Config file not found at: {LocalConfigPath}");
                    _cachedConfig = CreateDefaultConfig();
                    return _cachedConfig;
                }

                string json = File.ReadAllText(LocalConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _cachedConfig = config ?? new AppConfig();
                System.Diagnostics.Debug.WriteLine($"Config loaded: SnowEffect={_cachedConfig.SnowEffect}, Message={_cachedConfig.Message}");
                return _cachedConfig;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Config load error: {ex.Message}");
                _cachedConfig = new AppConfig();
                return _cachedConfig;
            }
        }

        /// <summary>
        /// Loads local config immediately for fast startup,
        /// then fetches remote config in background with timeout.
        /// If remote differs, caches it and fires RemoteConfigLoaded event.
        /// </summary>
        public static Task<AppConfig> LoadConfigAsync()
        {
            // Step 1: Load local config immediately for fast startup
            var localConfig = LoadConfig();

            // Step 2: Start background task to fetch remote config (fire-and-forget)
            _ = FetchRemoteConfigInBackgroundAsync(localConfig);

            return Task.FromResult(localConfig);
        }

        /// <summary>
        /// Fetches remote config in background. If successful and different from local,
        /// updates the cached config and fires RemoteConfigLoaded event.
        /// </summary>
        private static async Task FetchRemoteConfigInBackgroundAsync(AppConfig localConfig)
        {
            try
            {
                var remoteConfig = await FetchRemoteConfigAsync();
                if (remoteConfig != null && ConfigsDiffer(localConfig, remoteConfig))
                {
                    System.Diagnostics.Debug.WriteLine("Remote config differs from local. Applying remote settings.");
                    _cachedConfig = remoteConfig;
                    RemoteConfigLoaded?.Invoke(remoteConfig);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Remote config matches local or fetch returned null. No update needed.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Background remote config fetch failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetches configuration from remote GitHub URL with timeout.
        /// Returns null if fetch fails or times out.
        /// </summary>
        private static async Task<AppConfig> FetchRemoteConfigAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RemoteTimeoutSeconds));

                System.Diagnostics.Debug.WriteLine($"Fetching remote config from: {RemoteConfigUrl}");
                var json = await _httpClient.GetStringAsync(RemoteConfigUrl, cts.Token);

                var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                System.Diagnostics.Debug.WriteLine($"Remote config fetched: SnowEffect={config?.SnowEffect}, Message={config?.Message}");
                return config;
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Remote config fetch timed out after {RemoteTimeoutSeconds}s");
                return null;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Remote config fetch HTTP error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Remote config fetch error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Compares two configs to check if they differ in key settings.
        /// </summary>
        private static bool ConfigsDiffer(AppConfig local, AppConfig remote)
        {
            if (local == null || remote == null) return remote != null;

            return local.SnowEffect != remote.SnowEffect ||
                   local.Message != remote.Message ||
                   local.SnowflakeCount != remote.SnowflakeCount ||
                   Math.Abs(local.MinSpeed - remote.MinSpeed) > 0.01 ||
                   Math.Abs(local.MaxSpeed - remote.MaxSpeed) > 0.01 ||
                   Math.Abs(local.MinSize - remote.MinSize) > 0.01 ||
                   Math.Abs(local.MaxSize - remote.MaxSize) > 0.01;
        }

        /// <summary>
        /// Clears the cached config to force reload on next call.
        /// </summary>
        public static void ReloadConfig()
        {
            _cachedConfig = null;
        }

        /// <summary>
        /// Creates and saves a default config file if none exists.
        /// </summary>
        private static AppConfig CreateDefaultConfig()
        {
            var defaultConfig = new AppConfig
            {
                SnowEffect = true,
                Message = "Happy Holidays!",
                SnowflakeCount = 50,
                MinSpeed = 1.0,
                MaxSpeed = 4.0,
                MinSize = 3.0,
                MaxSize = 8.0
            };

            try
            {
                string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(LocalConfigPath, json);
                System.Diagnostics.Debug.WriteLine($"Default config created at: {LocalConfigPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create default config: {ex.Message}");
            }

            return defaultConfig;
        }
    }
}
