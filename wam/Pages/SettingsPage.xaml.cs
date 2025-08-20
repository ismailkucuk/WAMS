using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using wam; // MainWindow erişimi için
using wam.Services; // ExportService için

namespace wam.Pages
{
	public partial class SettingsPage : UserControl, ILoadablePage
	{
		private class WindowSettings
		{
			public bool MinimizeOnClose { get; set; }
		}

		public SettingsPage()
		{
			InitializeComponent();
			ExportControl.TargetPage = this;
			BtnClearDashboardCache.Click += BtnClearDashboardCache_Click;
			BtnOpenSettingsFolder.Click += BtnOpenSettingsFolder_Click;
			TglMinimizeOnClose.Checked += TglMinimizeOnClose_Changed;
			TglMinimizeOnClose.Unchecked += TglMinimizeOnClose_Changed;
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
				}
				else
				{
					TglMinimizeOnClose.IsChecked = false;
				}
			}
			catch { TglMinimizeOnClose.IsChecked = false; }
		}

		public void ExportToJson()
		{
			var export = new { MinimizeOnClose = TglMinimizeOnClose.IsChecked == true };
			ExportService.ExportToJson(new[] { export }, GetModuleName());
		}

		public void ExportToCsv()
		{
			var export = new[] { new { Key = "MinimizeOnClose", Value = TglMinimizeOnClose.IsChecked == true } };
			ExportService.ExportToCsv(export, GetModuleName());
		}

		public void AutoExport()
		{
			var export = new { MinimizeOnClose = TglMinimizeOnClose.IsChecked == true };
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
				var path = GetSettingsPath();
				var ws = new WindowSettings { MinimizeOnClose = TglMinimizeOnClose.IsChecked == true };
				var json = JsonSerializer.Serialize(ws, new JsonSerializerOptions { WriteIndented = true });
				await File.WriteAllTextAsync(path, json);

				// Anında uygulansın
				if (Application.Current?.MainWindow is MainWindow mw)
				{
					mw.ApplyMinimizeOnCloseSetting(ws.MinimizeOnClose);
				}
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
	}
}


