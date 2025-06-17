# WAM Uygulaması - Export Özellikleri

## 🎯 Genel Bakış

WAM (Windows Admin & Monitoring) uygulamasına her modül için otomatik JSON/CSV export fonksiyonları eklendi. Her sayfa sonunda kullanıcılar verilerini kolayca dışa aktarabilirler.

## 📦 Eklenen Bileşenler

### 1. **ExportService** (`/Services/ExportService.cs`)
- **JSON Export**: Veriyi JSON formatında dışa aktarır
- **CSV Export**: Veriyi CSV formatında dışa aktarır  
- **Auto Export**: Kullanıcı seçimi ile otomatik export
- **Silent Export**: Sessiz arka plan export'u

### 2. **ExportControl** (`/Controls/ExportControl.xaml`)
- Material Design uyumlu export butonları
- JSON, CSV ve Otomatik Export seçenekleri
- Her sayfada yeniden kullanılabilir kontrol

### 3. **AutoExportManager** (`/Services/AutoExportManager.cs`)
- Sayfa değişikliklerinde otomatik export yönetimi
- Ayarlanabilir otomatik export özellikleri
- Sessiz arka plan export işlemleri

### 4. **ILoadablePage Interface Genişletmesi**
```csharp
void ExportToJson();
void ExportToCsv(); 
void AutoExport();
string GetModuleName();
```

## 🚀 Export Desteği Eklenen Modüller

### ✅ **Tamamlanan Modüller**
1. **Dashboard** - Sistem durumu ve genel bilgiler
2. **UserSessionInfo** - Kullanıcı oturum bilgileri
3. **NetworkMonitor** - Ağ bağlantıları ve port izleme
4. **SystemInfo** - Sistem ve disk bilgileri
5. **UserActivity** - Kullanıcı aktivite logları

### 🔄 **Kolayca Eklenebilecek Modüller**
- SecurityPolicy - Güvenlik politikaları
- InstalledSoftware - Kurulu yazılım listesi
- EventLogAnalyzer - Event log analizi
- StartupPrograms - Başlangıç programları
- UsbMonitor - USB izleme
- FileSystemMonitor - Dosya sistemi izleme
- ActiveAppMonitor - Aktif uygulama izleme
- ProcessMonitor - Süreç izleme

## 📋 Export Formatları

### JSON Export Örneği
```json
{
  "ExportInfo": {
    "ModuleName": "Dashboard",
    "ExportDate": "2024-01-15 14:30:22",
    "TotalRecords": 1,
    "ExportedBy": "ADMIN",
    "MachineName": "PC-OFFICE-01"
  },
  "Data": [
    {
      "SystemMetrics": {
        "CpuUsage": 15,
        "UsedRamGB": 8.2,
        "TotalRamGB": 16.0,
        "Uptime": "2 gün, 4 saat"
      }
    }
  ]
}
```

### CSV Export Örneği
```csv
# Module: NetworkMonitor
# Export Date: 2024-01-15 14:30:22
# Exported By: ADMIN
# Machine: PC-OFFICE-01

ProcessId,ProcessName,LocalAddress,LocalPort,RemoteAddress,State,Protocol
1234,chrome.exe,192.168.1.100,54321,172.217.16.110,Established,TCP
5678,firefox.exe,192.168.1.100,49152,151.101.1.140,Established,TCP
```

## 🛠️ Kullanım

### Manuel Export
1. Herhangi bir sayfaya gidin
2. Sayfa sonundaki "Export" butonlarını kullanın:
   - **JSON**: JSON formatında kaydet
   - **CSV**: CSV formatında kaydet  
   - **Otomatik Export**: Kullanıcı seçimi ile kaydet

### Otomatik Export
```csharp
// Otomatik export'u etkinleştir
AutoExportManager.SetAutoExportEnabled(true);

// Export klasörünü ayarla
AutoExportManager.SetAutoExportDirectory(@"C:\WAM_Exports");
```

## 📁 Export Dosya Yapısı

```
Documents/WAM_Exports/
├── Dashboard_2024-01-15_14-30-22.json
├── Dashboard_2024-01-15_14-30-22.csv
├── UserSessionInfo_2024-01-15_14-31-45.json
├── UserSessionInfo_2024-01-15_14-31-45.csv
├── NetworkMonitor_2024-01-15_14-32-10.json
└── NetworkMonitor_2024-01-15_14-32-10.csv
```

## 🔧 Yeni Modüle Export Ekleme

### 1. Adım: ILoadablePage Interface'ini Implement Edin
```csharp
public partial class YeniSayfaPage : UserControl, ILoadablePage
{
    public void ExportToJson() { /* Implementasyon */ }
    public void ExportToCsv() { /* Implementasyon */ }  
    public void AutoExport() { /* Implementasyon */ }
    public string GetModuleName() => "YeniSayfa";
}
```

### 2. Adım: XAML'e ExportControl Ekleyin
```xml
<controls:ExportControl Grid.Row="0" x:Name="ExportControl"/>
```

### 3. Adım: Constructor'da Bağlantıyı Kurun
```csharp
public YeniSayfaPage()
{
    InitializeComponent();
    ExportControl.TargetPage = this;
}
```

## 📦 Gerekli NuGet Paketleri

```xml
<PackageReference Include="CsvHelper" Version="32.0.4" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## 🎨 UI/UX Özellikleri

- Material Design uyumlu butonlar
- Tooltip açıklamaları
- Error handling ve kullanıcı geri bildirimi
- Responsive tasarım
- Kolay erişilebilir butonlar

## 🔒 Güvenlik

- Export işlemleri kullanıcı izni ile gerçekleşir
- Dosya kaydetme dialog'ları güvenli
- Hassas veriler için ek kontroller eklenebilir

## 📈 Performans

- Asenkron export işlemleri
- Büyük veri setleri için memory-efficient yaklaşım
- Background processing desteği

## 🎯 Gelecek Geliştirmeler

- [ ] Zamanlı otomatik export (günlük, haftalık)
- [ ] Email ile export gönderimi
- [ ] Excel formatı desteği
- [ ] Export filtreleme seçenekleri
- [ ] Batch export işlemleri
- [ ] Cloud storage entegrasyonu

---

**Not**: Bu export sistemi modüler yapıda tasarlandığı için yeni sayfalara kolayca entegre edilebilir ve mevcut sayfalar için özelleştirilebilir. 