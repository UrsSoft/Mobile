# Firebase Push Notification Kurulum Kılavuzu

## Backend Firebase Admin SDK Kurulumu

### 1. Firebase Service Account Key Oluşturma

1. [Firebase Console](https://console.firebase.google.com/) adresine git
2. Projenizi seçin
3. **Project Settings** (⚙️ ikonu) → **Service Accounts** sekmesine git
4. **Generate New Private Key** butonuna tıklayın
5. İndirilen JSON dosyasını `firebase-service-account.json` olarak kaydedin

### 2. Service Account Dosyasını Backend'e Ekleme

Service account JSON dosyasını backend projesinin root klasörüne kopyalayın:

```
backend/
├── firebase-service-account.json  ← Buraya kopyalayın
├── Program.cs
├── appsettings.json
└── ...
```

**ÖNEMLİ**: Bu dosyayı `.gitignore`'a ekleyin! (Zaten ekli olmalı)

### 3. Backend Kurulumu

Backend projesinde Firebase Admin SDK paketi zaten yüklü:

```xml
<PackageReference Include="FirebaseAdmin" Version="3.0.1" />
```

### 4. PushNotificationService Konfigürasyonu

`PushNotificationService` otomatik olarak Firebase'i initialize eder:

```csharp
// Service account dosyası varsa
✅ Gerçek FCM push notification gönderilir

// Service account dosyası yoksa  
⚠️  Simüle edilir (log'larda görünür ama bildirim gitmez)
```

### 5. Test Etme

#### Backend Test
```bash
# Backend'i çalıştır
cd backend
dotnet run
```

Log'larda şu mesajı görmelisiniz:
```
Firebase Admin SDK initialized successfully
```

Eğer bu mesajı görmüyorsanız:
```
Firebase service account file not found. Push notifications will be simulated.
Expected path: /path/to/backend/firebase-service-account.json
```

#### Bildirim Gönderme Test
Mobile uygulamadan:
1. Login olun
2. FCM token otomatik kaydedilir
3. Backend'den bildirim gönderin (örn: yeni talep oluştur)
4. Push notification almalısınız!

## Firebase Console Ayarları

### Android Konfigürasyonu

1. Firebase Console → **Project Settings** → **General**
2. **Your apps** altında Android uygulamanızı bulun
3. **google-services.json** dosyasını indirin
4. `SantiyeTalepMobile/android/app/` klasörüne kopyalayın

### iOS Konfigürasyonu

1. Firebase Console → **Project Settings** → **Cloud Messaging**
2. **APNs Authentication Key** yükleyin
   - Apple Developer hesabınızdan APNs key oluşturun
   - `.p8` dosyasını Firebase'e yükleyin
3. **GoogleService-Info.plist** dosyasını indirin
4. `SantiyeTalepMobile/ios/` klasörüne kopyalayın

## Troubleshooting

### Push Notification Gönderilmiyor

1. **Backend tarafında kontrol edin:**
   ```bash
   # Log'larda şunları arayın:
   - "Firebase Admin SDK initialized successfully" ✅
   - "Successfully sent FCM notification. Message ID: ..." ✅
   - "Failed to send notification" ❌
   ```

2. **FCM Token kontrolü:**
   - Database'de kullanıcının `FcmToken` alanı dolu mu?
   - Mobile app'te login olduktan sonra console'da "FCM Token:" log'u görüyor musunuz?

3. **Service Account kontrolü:**
   ```bash
   # Backend dizininde dosya var mı?
   ls firebase-service-account.json
   
   # İçeriği doğru mu? (JSON formatında olmalı)
   cat firebase-service-account.json | head
   ```

4. **Firebase Console'da Cloud Messaging aktif mi?**
   - Project Settings → Cloud Messaging
   - API'ler aktif olmalı

### Simulated Mode'dan Çıkış

Eğer log'larda `[SIMULATED]` görüyorsanız:

1. ✅ `firebase-service-account.json` dosyasını ekleyin
2. ✅ Dosyanın backend root dizininde olduğundan emin olun
3. ✅ Backend'i yeniden başlatın
4. ✅ Log'larda "Firebase Admin SDK initialized successfully" mesajını kontrol edin

## Güvenlik Notları

### Service Account Dosyası
- ❌ **Asla GitHub'a push etmeyin**
- ❌ **Public repo'larda paylaşmayın**
- ✅ `.gitignore`'a eklenmiş olmalı
- ✅ Production'da environment variable veya secret management kullanın

### Production Deployment

Production ortamında service account bilgilerini environment variable olarak saklamak için:

```csharp
// Program.cs veya PushNotificationService.cs
var firebaseCredential = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
if (!string.IsNullOrEmpty(firebaseCredential))
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromJson(firebaseCredential)
    });
}
```

Azure App Service için:
1. Configuration → Application Settings
2. `FIREBASE_CREDENTIALS` key'i ekle
3. Service account JSON'unu value olarak yapıştır

## Örnek Kullanım

### Backend'den Notification Gönderme

```csharp
// Tek kullanıcıya bildirim
await _pushNotificationService.SendNotificationToUserAsync(
    userId: 1,
    title: "Yeni Talep",
    body: "Size yeni bir talep atandı",
    data: new { requestId = 123, type = 0 }
);

// Tedarikçiye bildirim
await _pushNotificationService.SendNotificationToSupplierAsync(
    supplierId: 5,
    title: "Teklif Onaylandı",
    body: "Teklifiniz onaylandı",
    data: new { offerId = 456 }
);

// Toplu bildirim
await _pushNotificationService.SendBulkNotificationAsync(
    fcmTokens: new List<string> { "token1", "token2" },
    title: "Sistem Bildirimi",
    body: "Önemli bir güncelleme var"
);
```

## Başarılı Kurulum Kontrol Listesi

- ✅ Firebase project oluşturuldu
- ✅ Android app Firebase'e eklendi (`google-services.json` indirildi)
- ✅ iOS app Firebase'e eklendi (`GoogleService-Info.plist` indirildi)
- ✅ Service account key oluşturuldu ve `firebase-service-account.json` olarak kaydedildi
- ✅ Backend'e `FirebaseAdmin` NuGet paketi eklendi
- ✅ `firebase-service-account.json` backend root dizinine kopyalandı
- ✅ `.gitignore` dosyasında `firebase-service-account.json` var
- ✅ Backend çalıştırıldığında "Firebase Admin SDK initialized successfully" log'u görünüyor
- ✅ Mobile app'te FCM token kaydediliyor
- ✅ Test bildirimi başarıyla gönderildi ve alındı

## Destek

Sorun yaşarsanız:
1. Backend log'larını kontrol edin
2. Mobile app console log'larını kontrol edin
3. Firebase Console → Cloud Messaging → Reports'u kontrol edin
4. Bu dokümandaki troubleshooting adımlarını takip edin
