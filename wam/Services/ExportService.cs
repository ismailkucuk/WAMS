using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using CsvHelper;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace wam.Services
{
    public static class ExportService
    {
        /// <summary>
        /// Veriyi JSON formatında dışa aktarır
        /// </summary>
        public static bool ExportToJson<T>(IEnumerable<T> data, string moduleName)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON dosyaları (*.json)|*.json",
                    Title = $"{moduleName} - JSON Export",
                    FileName = $"{moduleName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var jsonSettings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        DateFormatString = "yyyy-MM-dd HH:mm:ss",
                        NullValueHandling = NullValueHandling.Include
                    };

                    var jsonContent = JsonConvert.SerializeObject(new
                    {
                        ExportInfo = new
                        {
                            ModuleName = moduleName,
                            ExportDate = DateTime.Now,
                            TotalRecords = data is ICollection<T> collection ? collection.Count : -1,
                            ExportedBy = Environment.UserName,
                            MachineName = Environment.MachineName
                        },
                        Data = data
                    }, jsonSettings);

                    File.WriteAllText(saveFileDialog.FileName, jsonContent);
                    
                    MessageBox.Show(
                        $"JSON export başarılı!\nDosya: {saveFileDialog.FileName}", 
                        "Export Başarılı", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"JSON export hatası:\n{ex.Message}", 
                    "Export Hatası", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            return false;
        }

        /// <summary>
        /// Veriyi CSV formatında dışa aktarır
        /// </summary>
        public static bool ExportToCsv<T>(IEnumerable<T> data, string moduleName)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV dosyaları (*.csv)|*.csv",
                    Title = $"{moduleName} - CSV Export",
                    FileName = $"{moduleName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var writer = new StringWriter();
                    using var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("tr-TR"));
                    
                    csv.WriteRecords(data);
                    
                    File.WriteAllText(saveFileDialog.FileName, writer.ToString());
                    
                    MessageBox.Show(
                        $"CSV export başarılı!\nDosya: {saveFileDialog.FileName}", 
                        "Export Başarılı", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"CSV export hatası:\n{ex.Message}", 
                    "Export Hatası", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            return false;
        }

        /// <summary>
        /// Otomatik export - hem JSON hem CSV formatlarında
        /// </summary>
        public static void AutoExport<T>(IEnumerable<T> data, string moduleName)
        {
            if (data == null) return;

            var result = MessageBox.Show(
                $"{moduleName} modülü verilerini dışa aktarmak istiyor musunuz?\n\n" +
                "Evet: JSON ve CSV formatlarında kaydet\n" +
                "Hayır: Sadece JSON formatında kaydet\n" +
                "İptal: Export yapma",
                "Otomatik Export",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    ExportToJson(data, moduleName);
                    ExportToCsv(data, moduleName);
                    break;
                case MessageBoxResult.No:
                    ExportToJson(data, moduleName);
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }

        /// <summary>
        /// Sessiz otomatik export - kullanıcı onayı olmadan
        /// </summary>
        public static void SilentAutoExport<T>(IEnumerable<T> data, string moduleName, string baseDirectory = null)
        {
            try
            {
                if (data == null) return;

                string exportDir = baseDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WAM_Exports");
                Directory.CreateDirectory(exportDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                
                // JSON Export
                var jsonPath = Path.Combine(exportDir, $"{moduleName}_{timestamp}.json");
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    DateFormatString = "yyyy-MM-dd HH:mm:ss",
                    NullValueHandling = NullValueHandling.Include
                };

                var jsonContent = JsonConvert.SerializeObject(new
                {
                    ExportInfo = new
                    {
                        ModuleName = moduleName,
                        ExportDate = DateTime.Now,
                        TotalRecords = data is ICollection<T> collection ? collection.Count : -1,
                        ExportedBy = Environment.UserName,
                        MachineName = Environment.MachineName
                    },
                    Data = data
                }, jsonSettings);

                File.WriteAllText(jsonPath, jsonContent);

                // CSV Export
                var csvPath = Path.Combine(exportDir, $"{moduleName}_{timestamp}.csv");
                using var writer = new StringWriter();
                using var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("tr-TR"));
                
                csv.WriteRecords(data);
                File.WriteAllText(csvPath, writer.ToString());

                // Sadece debug amaçlı log
                System.Diagnostics.Debug.WriteLine($"Silent export completed for {moduleName}: JSON={jsonPath}, CSV={csvPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silent export error for {moduleName}: {ex.Message}");
            }
        }
    }
} 