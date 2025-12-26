using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Security.Principal;
using wam; // MainWindow erişimi için
using wam.Services; // ExportService için

namespace wam.Pages
{
	public partial class SettingsPage : UserControl, ILoadablePage
	{
		private class WindowSettings
		{
			public bool MinimizeOnClose { get; set; }
			public bool DarkTheme { get; set; }
			public string Language { get; set; } = "tr-TR";
		}

		public SettingsPage()
		{
			InitializeComponent();
			BtnClearDashboardCache.Click += BtnClearDashboardCache_Click;
			BtnOpenSettingsFolder.Click += BtnOpenSettingsFolder_Click;
			TglMinimizeOnClose.Checked += TglMinimizeOnClose_Changed;
			TglMinimizeOnClose.Unchecked += TglMinimizeOnClose_Changed;
			TglDarkTheme.Checked += TglDarkTheme_Changed;
			TglDarkTheme.Unchecked += TglDarkTheme_Changed;
			BtnRelaunchAsAdmin.Click += BtnRelaunchAsAdmin_Click;
			BtnRelaunchNormal.Click += BtnRelaunchNormal_Click;

			// Dil seçimi için ComboBox başlatma
			InitializeLanguageComboBox();
		}

		private void InitializeLanguageComboBox()
		{
			// Mevcut dile göre ComboBox'ı seç
			var currentLang = LocalizationService.Instance.CurrentLanguage;
			foreach (ComboBoxItem item in CmbLanguage.Items)
			{
				if (item.Tag?.ToString() == currentLang)
				{
					CmbLanguage.SelectedItem = item;
					break;
				}
			}
		}

		public async Task LoadDataAsync()
		{
			try
			{
				// Ayarları oku ve UI'a yansıt
				var path = GetSettingsPath();
				if (File.Exists(path))
				{
					var json = await File.ReadAllTextAsync(path);
					var s = JsonSerializer.Deserialize<WindowSettings>(json);
					TglMinimizeOnClose.IsChecked = s?.MinimizeOnClose ?? false;
					TglDarkTheme.IsChecked = s?.DarkTheme ?? false;
				}
				else
				{
					TglMinimizeOnClose.IsChecked = false;
					TglDarkTheme.IsChecked = false;
				}

				// Tema durumunu senkronize et
				TglDarkTheme.IsChecked = ThemeService.Instance.CurrentTheme == ThemeMode.Dark;

				// Oturum bilgisi
				TxtSessionRole.Text = IsRunningAsAdmin()
					? LocalizationService.Instance.GetString("Settings_Administrator", "Administrator")
					: LocalizationService.Instance.GetString("Settings_NormalUser", "Normal User");
			}
			catch 
			{ 
				TglMinimizeOnClose.IsChecked = false;
				TglDarkTheme.IsChecked = false;
			}
		}

		public void ExportToJson()
		{
			var export = new {
				MinimizeOnClose = TglMinimizeOnClose.IsChecked == true,
				DarkTheme = TglDarkTheme.IsChecked == true,
				Language = LocalizationService.Instance.CurrentLanguage
			};
			ExportService.ExportToJson(new[] { export }, GetModuleName());
		}

		public void ExportToCsv()
		{
			var export = new[] {
				new { Key = "MinimizeOnClose", Value = (TglMinimizeOnClose.IsChecked == true).ToString() },
				new { Key = "DarkTheme", Value = (TglDarkTheme.IsChecked == true).ToString() },
				new { Key = "Language", Value = LocalizationService.Instance.CurrentLanguage }
			};
			ExportService.ExportToCsv(export, GetModuleName());
		}

		public void AutoExport()
		{
			var export = new {
				MinimizeOnClose = TglMinimizeOnClose.IsChecked == true,
				DarkTheme = TglDarkTheme.IsChecked == true,
				Language = LocalizationService.Instance.CurrentLanguage
			};
			ExportService.AutoExport(new[] { export }, GetModuleName());
		}

		public string GetModuleName()
		{
			return "Settings";
		}

		private async void TglMinimizeOnClose_Changed(object sender, RoutedEventArgs e)
		{
			try
			{
				await SaveSettingsAsync();

				// Anında uygulansın
				if (Application.Current?.MainWindow is MainWindow mw)
				{
					mw.ApplyMinimizeOnCloseSetting(TglMinimizeOnClose.IsChecked == true);
				}
			}
			catch { }
		}

		private async void TglDarkTheme_Changed(object sender, RoutedEventArgs e)
		{
			try
			{
				var isDark = TglDarkTheme.IsChecked == true;
				await ThemeService.Instance.SetThemeAsync(isDark ? ThemeMode.Dark : ThemeMode.Light);
				await SaveSettingsAsync();
			}
			catch { }
		}

		private async void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (CmbLanguage.SelectedItem is ComboBoxItem selected && selected.Tag is string langCode)
				{
					await LocalizationService.Instance.SetLanguageAsync(langCode);
					await SaveSettingsAsync();
				}
			}
			catch { }
		}

		private async Task SaveSettingsAsync()
		{
			try
			{
				var path = GetSettingsPath();
				var ws = new WindowSettings
				{
					MinimizeOnClose = TglMinimizeOnClose.IsChecked == true,
					DarkTheme = TglDarkTheme.IsChecked == true,
					Language = LocalizationService.Instance.CurrentLanguage
				};
				var json = JsonSerializer.Serialize(ws, new JsonSerializerOptions { WriteIndented = true });
				await File.WriteAllTextAsync(path, json);
			}
			catch { }
		}

		private async void BtnClearDashboardCache_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAM");
				string cacheFile = Path.Combine(cacheDir, "dashboard_cache.json");
				if (File.Exists(cacheFile))
				{
					File.Delete(cacheFile);
					await Task.Delay(10);
				}
				MessageBox.Show("Dashboard önbelleği temizlendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Önbellek temizlenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BtnOpenSettingsFolder_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dir = Path.GetDirectoryName(GetSettingsPath());
				if (!string.IsNullOrEmpty(dir))
				{
					Directory.CreateDirectory(dir);
					System.Diagnostics.Process.Start("explorer.exe", dir);
				}
			}
			catch { }
		}

		private string GetSettingsPath()
		{
			var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAM");
			Directory.CreateDirectory(dir);
			return Path.Combine(dir, "settings.json");
		}

		private static bool IsRunningAsAdmin()
		{
			try
			{
				using var identity = WindowsIdentity.GetCurrent();
				var principal = new WindowsPrincipal(identity);
				return principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
			catch { return false; }
		}

		private void BtnRelaunchAsAdmin_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (IsRunningAsAdmin())
				{
					MessageBox.Show("Uygulama zaten yönetici modunda çalışıyor.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
				if (string.IsNullOrEmpty(exePath)) { MessageBox.Show("Yürütülebilir dosya yolu bulunamadı."); return; }

				var psi = new System.Diagnostics.ProcessStartInfo
				{
					FileName = exePath,
					UseShellExecute = true,
					Verb = "runas"
				};
				System.Diagnostics.Process.Start(psi);
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Yönetici olarak başlatılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BtnRelaunchNormal_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
				if (string.IsNullOrEmpty(exePath)) { MessageBox.Show("Yürütülebilir dosya yolu bulunamadı."); return; }

				var psi = new System.Diagnostics.ProcessStartInfo
				{
					FileName = exePath,
					UseShellExecute = true
				};
				System.Diagnostics.Process.Start(psi);
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Normal modda başlatılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}


