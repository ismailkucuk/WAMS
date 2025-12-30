# WAMS - Windows Activity Monitoring System

<div align="center">

![WAMS Logo](wam/Resources/logo_normal.png)

**Adli Bilişim ve Sistem İzleme için Geliştirilmiş Kapsamlı Windows Masaüstü Uygulaması**

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows)](https://docs.microsoft.com/dotnet/desktop/wpf/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-00ADEF?style=for-the-badge&logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.0.0-blue?style=for-the-badge)](https://github.com/ismailkucuk/WAMS/releases)

[Özellikler](#-özellikler) • [Kurulum](#-kurulum) • [Kullanım](#-kullanım) • [Ekran Görüntüleri](#-ekran-görüntüleri) • [Yapılandırma](#-yapılandırma) • [Katkıda Bulunma](#-katkıda-bulunma)

</div>

---

## 📋 İçindekiler

- [Hakkında](#-hakkında)
- [Özellikler](#-özellikler)
- [Sistem Gereksinimleri](#-sistem-gereksinimleri)
- [Kurulum](#-kurulum)
- [Kullanım](#-kullanım)
- [Modüller](#-modüller)
- [Yapılandırma](#-yapılandırma)
- [Veri Dışa Aktarım](#-veri-dışa-aktarım)
- [Teknik Detaylar](#-teknik-detaylar)
- [SSS](#-sss)
- [Katkıda Bulunma](#-katkıda-bulunma)
- [Lisans](#-lisans)

---

## 🎯 Hakkında

**WAMS (Windows Activity Monitoring System)**, Windows işletim sistemleri için geliştirilmiş kapsamlı bir sistem izleme ve adli bilişim aracıdır. Gerçek zamanlı sistem metrikleri, ağ bağlantıları, USB cihaz takibi, olay günlükleri ve kullanıcı aktiviteleri gibi kritik verileri tek bir arayüzden izlemenizi sağlar.

### Neden WAMS?

- **Tek Dosya Dağıtım** - Kurulum gerektirmez, tek çalıştırılabilir dosya
- **Gerçek Zamanlı İzleme** - CPU, RAM, GPU, Ağ metrikleri anlık güncellenir
- **Adli Bilişim Odaklı** - USB cihaz takibi, oturum açma/kapama logları, süreç zinciri analizi
- **Türkçe Arayüz** - Tam Türkçe dil desteği
- **Modern Tasarım** - Karanlık ve Aydınlık tema seçenekleri
- **Güvenlik Analizi** - Kritik port tespiti, güvenlik duvarı entegrasyonu

---

## ✨ Özellikler

### 🖥️ Gerçek Zamanlı İzleme

| Özellik | Açıklama |
|---------|----------|
| **Dashboard** | CPU, RAM, GPU kullanımı, ağ trafiği, güvenlik durumu özeti |
| **Aktif Uygulamalar** | Penceresi olan çalışan uygulamaların listesi |
| **Süreç Monitörü** | Tüm süreçler ve üst süreç (parent process) bilgisi |
| **Ağ Monitörü** | TCP bağlantıları, kritik port tespiti, port engelleme |
| **Dosya Sistemi** | Klasör değişikliklerini gerçek zamanlı izleme |
| **USB Monitörü** | USB cihaz takma/çıkarma olayları, port kontrolü |

### 🔍 Sistem Analizi

| Özellik | Açıklama |
|---------|----------|
| **Sistem Bilgisi** | Donanım, işletim sistemi, ağ bilgileri |
| **Başlangıç Programları** | Kayıt defteri ve zamanlanmış görevler |
| **Yüklü Yazılımlar** | Kurulu programlar ve kaldırma seçeneği |
| **Olay Günlüğü** | Windows Event Log analizi (Application, System, Security) |

### 👤 Kullanıcı ve Güvenlik

| Özellik | Açıklama |
|---------|----------|
| **Kullanıcı Aktiviteleri** | Oturum açma/kapama olayları |
| **Oturum Bilgisi** | Yerel kullanıcı yönetimi |
| **Güvenlik Politikaları** | Güvenlik değerlendirmesi ve öneriler |

### 🎨 Arayüz ve Deneyim

- **Karanlık/Aydınlık Tema** - Göz yorgunluğunu azaltan tema seçenekleri
- **Türkçe/İngilizce** - Çoklu dil desteği
- **Sistem Tepsisi** - Arka planda çalışma desteği
- **Otomatik Güncelleme** - Uygulama içi güncelleme kontrolü
- **Kış Teması** - Yapılandırılabilir kar efekti overlay'i

---

## 💻 Sistem Gereksinimleri

### Minimum Gereksinimler

| Bileşen | Gereksinim |
|---------|------------|
| **İşletim Sistemi** | Windows 10 (64-bit) veya üzeri |
| **İşlemci** | x64 uyumlu işlemci |
| **RAM** | 4 GB |
| **Disk Alanı** | 250 MB |
| **Ekran Çözünürlüğü** | 1366 x 768 |

### Önerilen Gereksinimler

| Bileşen | Gereksinim |
|---------|------------|
| **İşletim Sistemi** | Windows 11 (64-bit) |
| **İşlemci** | Intel Core i5 / AMD Ryzen 5 veya üzeri |
| **RAM** | 8 GB veya üzeri |
| **Disk Alanı** | 500 MB (önbellek dahil) |
| **Ekran Çözünürlüğü** | 1920 x 1080 veya üzeri |

### Yönetici Hakları Gerektiren Özellikler

Aşağıdaki özellikler için uygulamayı **Yönetici olarak çalıştırmanız** gerekmektedir:

- Güvenlik Olay Günlüğü okuma
- USB portlarını etkinleştirme/devre dışı bırakma
- Windows Güvenlik Duvarı kuralları ekleme/kaldırma
- Kullanıcı hesabı yönetimi (oluşturma, silme, şifre sıfırlama)

---

## 📥 Kurulum

### Yöntem 1: Hazır Sürüm (Önerilen)

1. [Releases](https://github.com/ismailkucuk/WAMS/releases) sayfasından en son sürümü indirin
2. ZIP dosyasını istediğiniz bir klasöre çıkarın
3. `WAMS.exe` dosyasını çalıştırın

> **Not:** Uygulama self-contained olarak paketlenmiştir, .NET Runtime kurulumu gerektirmez.

### Yöntem 2: Kaynak Koddan Derleme

```bash
# Depoyu klonlayın
git clone https://github.com/ismailkucuk/WAMS.git

# Proje klasörüne gidin
cd WAMS/wam

# Bağımlılıkları yükleyin ve derleyin
dotnet restore
dotnet build --configuration Release

# Tek dosya olarak yayınlayın
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Derlenen dosya `bin/Release/net8.0-windows/win-x64/publish/` klasöründe oluşturulacaktır.

---

## 🚀 Kullanım

### İlk Çalıştırma

1. `WAMS.exe` dosyasını çift tıklayarak başlatın
2. Uygulama otomatik olarak Dashboard sayfasıyla açılacaktır
3. Sol menüden istediğiniz modüle geçiş yapabilirsiniz

### Yönetici Modunda Çalıştırma

Bazı özellikler için yönetici hakları gereklidir:

1. **Ayarlar** sayfasına gidin
2. **"Yönetici Olarak Yeniden Başlat"** butonuna tıklayın
3. UAC (Kullanıcı Hesabı Denetimi) penceresinde izin verin

### Sistem Tepsisinde Çalıştırma

1. Pencereyi kapatırken açılan diyalogda **"Sistem tepsisine küçült"** seçeneğini seçin
2. **"Bu seçimi hatırla"** kutucuğunu işaretleyerek tercihinizi kaydedebilirsiniz
3. Sistem tepsisi simgesine çift tıklayarak pencereyi tekrar açabilirsiniz

> **İpucu:** `Shift` veya `Ctrl` tuşuna basılı tutarak pencereyi kapatırsanız, kaydedilmiş tercih geçersiz sayılır ve diyalog tekrar gösterilir.

---

## 📊 Modüller

### Dashboard

Ana kontrol paneli, sistemin genel durumunu tek bakışta görmenizi sağlar.

**Gösterilen Metrikler:**
- CPU Kullanımı (%) ve işlemci adı
- RAM Kullanımı (GB) ve toplam bellek
- GPU Kullanımı (%) ve ekran kartı adı
- Ağ Trafiği (Mbps) - indirme/yükleme grafiği
- Aktif Bağlantı ve Dinleyen Port sayısı
- Güvenlik Durumu özeti
- Sistem Çalışma Süresi (Uptime)
- Son Aktiviteler listesi

**Performans Özellikleri:**
- Dashboard verileri önbelleğe alınır, hızlı yeniden yükleme sağlanır
- 5 saniyede bir otomatik güncelleme
- Peak (tepe) değerleri takibi

---

### Aktif Uygulamalar

Şu anda çalışan ve görünür penceresi olan uygulamaları listeler.

**Gösterilen Bilgiler:**
- Uygulama adı
- Süreç ID (PID)
- Başlangıç zamanı
- Bellek kullanımı
- CPU kullanımı

**Sıralama Seçenekleri:**
- Ada göre
- Bellek kullanımına göre
- CPU kullanımına göre

---

### Süreç Monitörü

Sistemde çalışan tüm süreçleri detaylı olarak listeler.

**Özellikler:**
- Süreç ID, Ad, Başlangıç Zamanı
- **Üst Süreç Analizi** - Her sürecin hangi süreç tarafından başlatıldığını gösterir
- Süreç zinciri görselleştirmesi
- Adli bilişim için kritik bilgiler

---

### Ağ Monitörü

Aktif TCP bağlantılarını gerçek zamanlı izler.

**Gösterilen Bilgiler:**
- Süreç ID ve Adı
- Yerel/Uzak IP Adresi ve Port
- Bağlantı Durumu (Established, Listen, Time_Wait, vb.)
- Uzak Sunucu Domain Adı (Ters DNS çözümlemesi)
- Risk Etiketi (Kritik portlar için)

**Filtreler:**
- Tüm Bağlantılar
- Dinleyen Portlar
- Kritik Portlar (21, 22, 23, 25, 53, 80, 139, 443, 445, 3389)

**Port Yönetimi:**
- Tehlikeli portları Windows Güvenlik Duvarı üzerinden engelleyebilirsiniz
- Engellenen portları tekrar açabilirsiniz

> **Uyarı:** Port engelleme işlemi yönetici hakları gerektirir.

---

### Dosya Sistemi Monitörü

Seçilen klasördeki dosya değişikliklerini gerçek zamanlı izler.

**İzlenen Olaylar:**
- Dosya Oluşturma
- Dosya Değiştirme
- Dosya Silme
- Dosya Yeniden Adlandırma

**Kullanım:**
1. İzlemek istediğiniz klasörü seçin
2. "İzlemeyi Başlat" butonuna tıklayın
3. Değişiklikler anlık olarak listelenecektir

---

### USB Monitörü

USB depolama cihazlarının takılma/çıkarılma olaylarını izler ve USB portlarını yönetir.

**Özellikler:**
- Gerçek zamanlı USB cihaz takibi
- Cihaz bilgileri: Model, Üretici, Sürücü Harfi, Kapasite, Dosya Sistemi
- Donanım Kimliği (PNPDeviceID) kaydı
- USB portlarını etkinleştirme/devre dışı bırakma

**Adli Bilişim Değeri:**
- Her USB cihazın tam kimlik bilgisi loglanır
- Takma/çıkarma zamanları kaydedilir
- Veri sızıntısı tespiti için kritik önem taşır

> **Uyarı:** USB port kontrolü yönetici hakları ve kayıt defteri değişikliği gerektirir.

---

### Sistem Bilgisi

Sistem hakkında kapsamlı donanım ve yazılım bilgileri sunar.

**Gösterilen Bilgiler:**
- Kullanıcı Adı ve Bilgisayar Adı
- IP Adresi
- İşletim Sistemi ve Mimari (x64/x86)
- Toplam ve Kullanılabilir RAM
- Domain Bilgisi
- BIOS Sürümü
- Disk Kullanımı (görsel gösterim)

---

### Başlangıç Programları

Sistem başlangıcında otomatik çalışan programları listeler.

**Taranan Konumlar:**
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (Mevcut Kullanıcı)
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Run` (Tüm Kullanıcılar)
- `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup` (Başlangıç Klasörü)
- **Zamanlanmış Görevler** - Sistem başlangıcında veya oturum açılışında çalışan görevler

**Gösterilen Bilgiler:**
- Program Adı
- Çalıştırılabilir Dosya Yolu
- Kaynak (Registry/Klasör/Zamanlanmış Görev)
- Son Çalışma ve Sonraki Çalışma Zamanı (görevler için)

---

### Yüklü Yazılımlar

Sistemde kurulu olan programları listeler.

**Özellikler:**
- Program Adı, Yayıncı, Sürüm, Boyut
- Sıralama seçenekleri (Ad, Yayıncı, Boyut)
- Doğrudan kaldırma seçeneği

---

### Olay Günlüğü Analizi

Windows Event Log'larını görüntüler ve analiz eder.

**Desteklenen Günlükler:**
- **Application** - Uygulama olayları
- **System** - Sistem olayları
- **Security** - Güvenlik olayları (Yönetici gerektirir)

**Gösterilen Bilgiler:**
- Zaman Damgası
- Kaynak
- Olay Türü (Information, Warning, Error)
- Olay ID
- Mesaj Detayı

**Özellikler:**
- Son 500 olay listelenir
- Olay detaylarını ayrı pencerede görüntüleme
- Mesajı panoya kopyalama
- Online arama (Event ID ile)

---

### Kullanıcı Aktiviteleri

Güvenlik günlüğünden oturum açma/kapama olaylarını listeler.

**İzlenen Olaylar:**
- **Event ID 4624** - Başarılı Oturum Açma
- **Event ID 4634** - Oturum Kapatma

**Gösterilen Bilgiler:**
- Zaman
- Kullanıcı Adı
- Olay Türü (Login/Logout)
- Oturum Türü
- Kaynak IP Adresi

> **Not:** Bu modül Güvenlik günlüğünü okuduğu için yönetici hakları gerektirir.

---

### Kullanıcı Oturum Bilgisi

Yerel kullanıcı hesaplarını yönetir.

**Gösterilen Bilgiler:**
- Kullanıcı Adı
- Rol (Yönetici/Standart)
- Hesap Durumu
- Son Oturum Açma Zamanı
- Grup Üyelikleri

**Yönetim İşlemleri (Yönetici Gerektirir):**
- Şifre Sıfırlama
- Hesabı Etkinleştir/Devre Dışı Bırak
- Yönetici Yap/Kaldır
- Kullanıcı Sil
- Yeni Kullanıcı Ekle

---

### Güvenlik Politikaları

Sistem güvenlik durumunu değerlendirir.

**Analiz Edilen Alanlar:**
- Kritik Sorunlar
- Uyarılar
- Başarılı Kontroller
- Güvenlik Puanı

**Öneriler:**
- Tespit edilen sorunlar için çözüm önerileri
- "Düzelt" butonları ile hızlı aksiyon alma

---

### Ayarlar

Uygulama tercihlerini yönetir.

**Genel Ayarlar:**
- Karanlık/Aydınlık Tema
- Dil Seçimi (Türkçe/İngilizce)
- Kapatırken Sistem Tepsisine Küçült

**Yönetici Modu:**
- Mevcut oturum durumu
- Yönetici olarak yeniden başlatma

**Önbellek:**
- Dashboard önbelleğini temizle
- Ayarlar klasörünü aç

**Güncellemeler:**
- Mevcut sürüm bilgisi
- Güncelleme kontrolü

---

## ⚙️ Yapılandırma

### Uygulama Yapılandırması

`local_config.json` dosyası uygulama klasöründe bulunur:

```json
{
  "snow_effect": true,
  "message": "Happy Holidays from WAMS!",
  "snowflake_count": 75,
  "min_speed": 1.0,
  "max_speed": 4.0,
  "min_size": 3.0,
  "max_size": 8.0
}
```

| Anahtar | Tür | Açıklama |
|---------|-----|----------|
| `snow_effect` | boolean | Kar efekti overlay'ini etkinleştirir |
| `message` | string | Tatil mesajı |
| `snowflake_count` | integer | Kar tanesi sayısı (30-200 arası) |
| `min_speed` / `max_speed` | double | Kar tanesi düşüş hızı aralığı |
| `min_size` / `max_size` | double | Kar tanesi boyut aralığı (piksel) |

### Kullanıcı Ayarları

Kullanıcı ayarları `%LocalAppData%\WAM\` klasöründe saklanır:

| Dosya | İçerik |
|-------|--------|
| `settings.json` | Pencere davranışı (küçültme tercihi) |
| `theme_settings.json` | Tema tercihi (Dark/Light) |
| `language_settings.json` | Dil tercihi (tr-TR/en-US) |
| `dashboard_cache.json` | Dashboard önbelleği |

### Log Dosyaları

Hata logları `%LocalAppData%\WAM\Logs\` klasöründe saklanır:
- Format: `crash_{tarih}_{saat}_{kaynak}.log`
- Beklenmeyen hatalar otomatik olarak loglanır

---

## 📤 Veri Dışa Aktarım

Tüm modüller veri dışa aktarımını destekler.

### Desteklenen Formatlar

| Format | Açıklama |
|--------|----------|
| **JSON** | Yapılandırılmış veri, metadata içerir (modül adı, tarih, kayıt sayısı, kullanıcı, makine adı) |
| **CSV** | Tablo formatı, Excel ile uyumlu |

### Dışa Aktarım Yöntemleri

1. **Manuel Dışa Aktarım**
   - Her sayfada bulunan "Dışa Aktar" butonunu kullanın
   - JSON veya CSV formatını seçin
   - Kayıt konumunu belirleyin

2. **Otomatik Dışa Aktarım**
   - "Otomatik Dışa Aktar" seçeneği her iki formatı birden oluşturur

3. **Sessiz Otomatik Dışa Aktarım**
   - Yapılandırıldığında, sayfa yüklendiğinde otomatik olarak dışa aktarım yapar
   - Varsayılan konum: `Belgelerim\WAM_Exports\`

### Dışa Aktarım Örneği (JSON)

```json
{
  "ExportInfo": {
    "ModuleName": "NetworkMonitor",
    "ExportDate": "2024-12-31T14:30:00",
    "TotalRecords": 45,
    "ExportedBy": "admin",
    "MachineName": "WORKSTATION-01"
  },
  "Data": [
    {
      "ProcessId": 1234,
      "ProcessName": "chrome",
      "LocalAddress": "192.168.1.100",
      "LocalPort": 52341,
      "RemoteAddress": "142.250.185.78",
      "RemoteDomain": "www.google.com",
      "State": "Established",
      "Protocol": "TCP"
    }
  ]
}
```

---

## 🔧 Teknik Detaylar

### Kullanılan Teknolojiler

| Teknoloji | Sürüm | Kullanım Alanı |
|-----------|-------|----------------|
| .NET | 8.0 | Uygulama framework'ü |
| WPF | - | Kullanıcı arayüzü |
| C# | 12 | Programlama dili |

### NuGet Paketleri

| Paket | Sürüm | Açıklama |
|-------|-------|----------|
| AutoUpdater.NET.Official | 1.9.2 | Otomatik güncelleme |
| LiveCharts.Wpf | 0.9.7 | Grafikler ve göstergeler |
| MaterialDesignThemes | 5.2.1 | Material Design bileşenleri |
| Microsoft.ML | 4.0.2 | Makine öğrenmesi (gelecek özellikler için) |
| Microsoft.ML.FastTree | 4.0.2 | ML algoritmaları |
| Microsoft.ML.TimeSeries | 4.0.2 | Zaman serisi analizi |
| Ookii.Dialogs.Wpf | 5.0.1 | Modern klasör seçici |
| System.DirectoryServices.AccountManagement | 9.0.6 | Kullanıcı yönetimi |
| System.Management | 9.0.6 | WMI sorguları |
| TaskScheduler | 2.12.1 | Zamanlanmış görevler API'si |
| CsvHelper | 32.0.4 | CSV dosya oluşturma |
| Newtonsoft.Json | 13.0.3 | JSON serileştirme |

### Sistem API'leri

- **PerformanceCounter** - CPU, RAM, GPU, Ağ metrikleri
- **IPGlobalProperties** - TCP bağlantı numaralandırması
- **EventLog** - Windows Olay Günlüğü
- **ManagementObjectSearcher** - WMI sorguları
- **ManagementEventWatcher** - Gerçek zamanlı WMI olayları
- **Registry** - Kayıt defteri erişimi
- **netsh** - Windows Güvenlik Duvarı yönetimi

### Dağıtım Modeli

```
Tek Dosya + Self-Contained + ReadyToRun
```

- **Tek Dosya:** Tüm bağımlılıklar tek EXE'de paketlenir
- **Self-Contained:** .NET Runtime dahil edilir, kurulum gerektirmez
- **ReadyToRun:** AOT derleme ile hızlı başlangıç

---

## ❓ SSS

### Uygulama neden yönetici hakları istiyor?

Bazı özellikler (Güvenlik günlüğü, USB port kontrolü, Kullanıcı yönetimi, Port engelleme) Windows güvenlik politikaları gereği yönetici hakları gerektirir. Temel izleme özellikleri yönetici olmadan da çalışır.

### Verilerim nerede saklanıyor?

- Ayarlar: `%LocalAppData%\WAM\`
- Dışa aktarımlar: `Belgelerim\WAM_Exports\`
- Hata logları: `%LocalAppData%\WAM\Logs\`

### Kar efektini nasıl kapatabilirim?

`local_config.json` dosyasında `"snow_effect": false` yapın veya dosyayı silin.

### Uygulama arka planda çalışabilir mi?

Evet, pencereyi kapatırken "Sistem tepsisine küçült" seçeneğini seçin. Sistem tepsisi simgesinden tekrar açabilirsiniz.

### Güncelleme nasıl yapılır?

Ayarlar sayfasındaki "Güncellemeleri Kontrol Et" butonunu kullanın veya uygulama başlangıçta otomatik olarak kontrol eder.

### Dil nasıl değiştirilir?

Ayarlar sayfasından Dil seçeneğini kullanarak Türkçe ve İngilizce arasında geçiş yapabilirsiniz.

---

## 🤝 Katkıda Bulunma

Katkılarınızı memnuniyetle karşılıyoruz!

### Nasıl Katkıda Bulunabilirsiniz?

1. Bu depoyu fork edin
2. Feature branch oluşturun (`git checkout -b feature/YeniOzellik`)
3. Değişikliklerinizi commit edin (`git commit -m 'Yeni özellik eklendi'`)
4. Branch'inizi push edin (`git push origin feature/YeniOzellik`)
5. Pull Request açın

### Hata Bildirimi

Bir hata bulduysanız:
1. [Issues](https://github.com/ismailkucuk/WAMS/issues) sayfasını kontrol edin
2. Aynı hata daha önce bildirilmediyse yeni bir issue açın
3. Hatayı detaylı açıklayın (adımlar, beklenen/gerçekleşen davranış, ekran görüntüsü)

### Geliştirme Ortamı

- Visual Studio 2022 veya üzeri
- .NET 8.0 SDK
- Windows 10/11 (64-bit)

---

## 📄 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

---

## 👨‍💻 Geliştirici

**İsmail Küçük**

- GitHub: [@ismailkucuk](https://github.com/ismailkucuk)
- LinkedIn: [@İsmail Küçük](https://linkedin.com/in/ismail-küçük)

---

## 🙏 Teşekkürler

Bu projede kullanılan açık kaynak kütüphanelerin geliştiricilerine teşekkür ederiz:
- [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)
- [Live-Charts](https://github.com/Live-Charts/Live-Charts)
- [Material Design In XAML Toolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [CsvHelper](https://github.com/JoshClose/CsvHelper)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)

---

<div align="center">

**WAMS v1.0.0** | Windows Activity Monitoring System

Adli Bilişim ve Sistem Güvenliği için Geliştirildi

</div>
