# WAM (Windows Activity Monitor) Deployment Guide

## 🚀 Uygulamayı Başka Cihazlarda Çalıştırma

Bu rehber, WAM uygulamasını farklı Windows bilgisayarlarında nasıl çalıştırabileceğinizi açıklar.

## 📋 Gereksinimler

### Hedef Bilgisayar Gereksinimleri:
- **İşletim Sistemi**: Windows 10/11 (x64 veya x86)
- **Yönetici Hakları**: Bazı sistem bilgileri için gerekli
- **.NET Runtime**: Self-contained deployment ile gerekli değil

## 🎯 Dağıtım Yöntemleri

### Yöntem 1: Otomatik Dağıtım (Önerilen)

1. **Deploy Script Çalıştırma:**
   ```bash
   deploy.bat
   ```

2. **Sonuç Klasörleri:**
   - `deploy/windows-x64/` - 64-bit Windows için
   - `deploy/windows-x86/` - 32-bit Windows için

3. **Dağıtım:**
   - Uygun klasörü hedef bilgisayara kopyalayın
   - `wam.exe` dosyasını çalıştırın

### Yöntem 2: Manuel Dağıtım

```bash
# x64 için
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# x86 için  
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true
```

## 📊 Hangi Veriler Görüntülenir?

WAM uygulaması her cihazda **o cihaza özgü** gerçek zamanlı verileri gösterir:

### 🖥️ Sistem Performansı
- CPU kullanımı
- RAM kullanımı
- Disk kullanımı

### 🔄 Süreç İzleme
- Çalışan uygulamalar
- Sistem süreçleri
- Bellek kullanımı

### 🌐 Ağ İzleme
- Aktif bağlantılar
- Port kullanımı
- Ağ trafiği

### 🔒 Güvenlik ve Kullanıcı Aktivitesi
- Kullanıcı oturum bilgileri
- Sistem olayları
- USB cihaz aktivitesi

### 📁 Dosya Sistemi
- Dosya değişiklikleri
- Sistem dosyaları
- Yüklü yazılımlar

## ⚡ Çalıştırma Adımları

1. **Uygulamayı Kopyala**: Deploy klasörünü hedef bilgisayara kopyalayın
2. **Yönetici Olarak Çalıştır**: `wam.exe`'ye sağ tıklayıp "Yönetici olarak çalıştır" seçin
3. **İzinleri Onayla**: Windows güvenlik uyarılarını onaylayın
4. **Verileri Görüntüle**: Her cihaz kendi verilerini gösterecek

## 🛠️ Sorun Giderme

### Uygulama Açılmıyor
- Yönetici hakları ile çalıştırmayı deneyin
- Windows Defender'ı geçici olarak devre dışı bırakın
- Antivirüs yazılımına istisna ekleyin

### Bazı Veriler Görünmüyor
- UAC (User Account Control) açık olmalı
- Performance Counter servisleri aktif olmalı
- WMI servisi çalışıyor olmalı

### Performans Sorunları
- Veri yenileme sıklığını azaltın
- Gereksiz modülleri kapatın
- RAM ve CPU kullanımını kontrol edin

## 🔐 Güvenlik Notları

- Uygulama sistem bilgilerine erişim gerektirir
- Hassas veriler yerel olarak kalır
- Ağ üzerinden veri aktarımı yoktur
- Her cihaz sadece kendi verilerini gösterir

## 📂 Dosya Yapısı

```
deploy/
├── windows-x64/
│   ├── wam.exe          # Ana uygulama
│   ├── Resources/       # Görseller ve ikonlar
│   └── ...             # Bağımlılıklar (otomatik dahil)
└── windows-x86/
    ├── wam.exe
    ├── Resources/
    └── ...
```

## 🎯 Sonuç

Bu yöntemle WAM uygulamasını herhangi bir Windows bilgisayarında çalıştırabilir ve o bilgisayara özgü gerçek zamanlı sistem verilerini izleyebilirsiniz. Her cihaz kendi verilerini gösterecek ve merkezi bir kurulum gerektirmeyecektir. 