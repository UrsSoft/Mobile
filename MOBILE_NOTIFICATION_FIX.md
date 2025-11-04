# 🚨 Mobil Bildirim Sorunu ve Çözümü

## Sorun
WebUI'dan admin tedarikçiye talep gönderdiğinde, **mobil uygulama kapalıyken bildirim gelmiyor**.

## Neden Gelmiyor?
Backend'de **Firebase service account dosyası eksik** olduğu için:
- ✅ Database'e notification kaydediliyor
- ❌ Gerçek FCM push notification gönderilmiyor (simulated mode)
- ❌ Uygulama kapalıyken sadece FCM push ile bildirim gelebilir

Backend log'unda şu mesaj görünüyor olmalı:
```
Firebase service account file not found. Push notifications will be simulated.
```

## 🔥 ÇÖZÜM: Firebase Admin SDK Kurulumu

### Adım 1: Firebase Service Account Key Oluştur

1. **Firebase Console'a git**: https://console.firebase.google.com/
2. Projenizi seçin (SantiyeTalepMobile veya benzer)
3. ⚙️ **Project Settings** → **Service Accounts** sekmesi
4. **Generate New Private Key** butonuna tıkla
5. Açılan uyarıda **Generate Key** onaylayın
6. İndirilen JSON dosyasını kaydedin

### Adım 2: Dosyayı Backend'e Ekle

```powershell
# Backend dizinine git
cd D:\ElementElektrik\Mobile\backend

# İndirdiğiniz JSON dosyasını firebase-service-account.json olarak kopyalayın
# Örnek:
Copy-Item "C:\Users\YourName\Downloads\santiyetalep-firebase-adminsdk-xxxxx.json" "firebase-service-account.json"
```

**ÖNEMLİ**: Dosya adı tam olarak `firebase-service-account.json` olmalı!

### Adım 3: Backend'i Yeniden Başlat

```powershell
# Backend'i durdur (Ctrl+C)
# Sonra yeniden başlat
cd D:\ElementElektrik\Mobile\backend
dotnet run
```

### Adım 4: Log'ları Kontrol Et

Backend başladığında log'larda şunu görmelisiniz:
```
Firebase Admin SDK initialized successfully
```

### Adım 5: Test Et

1. **Mobile uygulamaya login olun** (FCM token kaydedilecek)
2. **Uygulamayı tamamen kapatın** (Recent apps'ten kaydırın)
3. **WebUI'dan tedarikçiye talep gönderin**
4. **📱 Bildirim gelecek!** (Ses + titreşim)

## 🔍 Sorun Giderme

### Backend Log'larında Hata Varsa

```powershell
# Backend log'larını dikkatli okuyun
cd D:\ElementElektrik\Mobile\backend
dotnet run
```

Şu log'ları arayın:
- ✅ `"Firebase Admin SDK initialized successfully"` → İyi!
- ❌ `"Firebase service account file not found"` → Dosya yok veya yanlış yerde
- ❌ `"Failed to initialize Firebase Admin SDK"` → JSON dosyası bozuk

### Firebase Service Account Dosyası Doğru mu?

```powershell
# Dosyanın varlığını kontrol et
cd D:\ElementElektrik\Mobile\backend
Test-Path firebase-service-account.json

# Dosya içeriğini kontrol et (ilk 10 satır)
Get-Content firebase-service-account.json | Select-Object -First 10
```

Dosya şöyle görünmeli:
```json
{
  "type": "service_account",
  "project_id": "santiyetalep-xxxxx",
  "private_key_id": "...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...",
  ...
}
```

### FCM Token Kaydedildi mi?

Mobil uygulamada console log'larına bakın:
```
FCM Token: ey...
FCM token registered successfully
```

Backend'de database'i kontrol edin:
```sql
SELECT Id, Email, Phone, FcmToken FROM Users WHERE Role = 'Supplier'
```

`FcmToken` kolonu dolu olmalı.

## 📊 Test Sonuçları

Başarılı kurulum sonrası:

| Durum | Sonuç |
|-------|-------|
| Uygulama açık | ✅ Bildirim gelir |
| Uygulama arka planda | ✅ Bildirim gelir |
| Uygulama kapalı | ✅ Bildirim gelir |
| Ses | ✅ Çalar |
| Titreşim | ✅ Aktif |

## 🚀 Hızlı Test Komutu

Firebase service account ekledikten sonra:

```powershell
# 1. Backend'i çalıştır
cd D:\ElementElektrik\Mobile\backend
dotnet run

# Yeni terminal aç
# 2. Mobile uygulamayı başlat (başka terminal)
cd D:\ElementElektrik\Mobile\SantiyeTalepMobile
npx react-native run-android

# 3. Login ol
# 4. Uygulamayı kapat
# 5. WebUI'dan talep gönder
# 6. Bildirim gelecek! 🎉
```

## ⚠️ Güvenlik Notları

- ❌ **firebase-service-account.json dosyasını GitHub'a push ETMEYİN**
- ✅ Dosya `.gitignore`'da olmalı (zaten ekli)
- ✅ Production'da environment variable kullanın

## 💡 Alternatif: Simulated Mode'u Test Et

Firebase olmadan test etmek için (sadece uygulama açıkken çalışır):

```csharp
// Backend/Services/PushNotificationService.cs
// _isFirebaseInitialized = false durumunda
// Log'larda [SIMULATED] görürsünüz ama gerçek bildirim gitmez
```

**NOT**: Simulated mode'da uygulama kapalıyken bildirim GELMEYECEKTİR çünkü FCM push gönderilmez!

---

## 📝 Özet

**Sorun**: Firebase service account eksik  
**Çözüm**: `firebase-service-account.json` dosyasını backend'e ekle  
**Süre**: ~5 dakika  
**Sonuç**: Uygulama kapalıyken bile bildirim gelecek! 🎊
