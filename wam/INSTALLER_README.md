# 📦 WAM Installer Oluşturma ve Kullanma Rehberi

## 🎯 Amaç
Bu rehber, WAM uygulamanız için tek dosya Windows installer oluşturmanızı sağlar. Böylece kullanıcılara sadece bir `.exe` dosyası gönderip, çift tıklayarak kurulum yapmalarını sağlayabilirsiniz.

## 🛠️ Gereksinimler

### 1. NSIS Kurulumu
```bash
# NSIS'i şu adresten indirin ve kurun:
https://nsis.sourceforge.io/Download

# Kurulum sonrası PATH'e ekleyin veya installer oluştururken tam yol kullanın
```

### 2. .NET SDK
- .NET 8.0 SDK yüklü olmalı
- `dotnet` komutu PATH'de bulunmalı

## 🚀 Installer Oluşturma

### Otomatik Yöntem (Önerilen)
```bash
# Tek komutla installer oluşturun:
build_installer.bat
```

### Manuel Yöntem
```bash
# 1. Uygulamayı derleyin
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 2. NSIS ile installer oluşturun
makensis create_installer.nsi
```

## 📋 Installer Özellikleri

### ✅ Neler Yapılır?
- ✅ Program Files klasörüne kurulum
- ✅ Start Menu'ye kısayol ekleme
- ✅ Desktop'a kısayol oluşturma
- ✅ Uninstaller oluşturma
- ✅ Registry kayıtları (Add/Remove Programs)
- ✅ Lisans anlaşması gösterme
- ✅ Yönetici hakları kontrolü

### 📂 Kurulum Konumu
```
C:\Program Files\Windows Activity Monitor\
├── wam.exe                    # Ana uygulama
├── Resources\                 # Görseller ve ikonlar
│   ├── wams_logo.png
│   ├── wams.ico
│   └── pngwing.com.png
└── uninstall.exe             # Kaldırma programı
```

### 🔗 Kısayollar
- **Start Menu**: `Start Menu → Windows Activity Monitor`
- **Desktop**: `Desktop → Windows Activity Monitor`
- **Uninstall**: `Start Menu → Windows Activity Monitor → Uninstall`

## 📤 Dağıtım

### 1. Installer Oluşturma
```bash
build_installer.bat
```

### 2. Dosya Kontrolü
- ✅ `WAM_Installer.exe` oluşturuldu mu?
- ✅ Dosya boyutu uygun mu? (~40-50 MB)

### 3. Test Etme
- ✅ Installer'ı test bilgisayarında çalıştırın
- ✅ Kurulum tamamlandı mı?
- ✅ Uygulama düzgün açılıyor mu?
- ✅ Kaldırma işlemi çalışıyor mu?

## 👥 Kullanıcı Talimatları

### Kurulum
1. **WAM_Installer.exe** dosyasını indirin
2. Dosyaya **sağ tıklayın** → **"Yönetici olarak çalıştır"**
3. **Lisans anlaşmasını** okuyup kabul edin
4. **Kurulum konumunu** seçin (varsayılan önerilir)
5. **Install** butonuna tıklayın
6. Kurulum tamamlandığında **Finish** yapın

### Çalıştırma
- **Start Menu** → **Windows Activity Monitor**
- **Desktop'taki kısayol** → **Windows Activity Monitor**

### Kaldırma
- **Settings** → **Apps** → **Windows Activity Monitor** → **Uninstall**
- veya **Start Menu** → **Windows Activity Monitor** → **Uninstall**

## 🔧 Sorun Giderme

### NSIS Bulunamıyor
```bash
# Çözüm 1: PATH'e ekleyin
set PATH=%PATH%;C:\Program Files (x86)\NSIS

# Çözüm 2: Tam yol kullanın
"C:\Program Files (x86)\NSIS\makensis.exe" create_installer.nsi
```

### Publish Hatası
```bash
# .NET SDK'nın güncel olduğundan emin olun
dotnet --version

# Proje dosyalarını temizleyin
dotnet clean
```

### İzin Hatası
- PowerShell'i yönetici olarak çalıştırın
- Antivirüs yazılımını geçici olarak devre dışı bırakın

## 🎁 Sonuç

Bu installer sistemi ile:
- ✅ **Tek dosya** göndererek dağıtım yapabilirsiniz
- ✅ **Profesyonel görünüm** elde edersiniz
- ✅ **Kolay kurulum** sağlarsınız
- ✅ **Temiz kaldırma** mümkün olur
- ✅ **Sistem entegrasyonu** (Start Menu, Desktop) otomatik olur

Artık WAM uygulamanızı herkesle kolayca paylaşabilirsiniz! 🚀 