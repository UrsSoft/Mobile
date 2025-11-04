# 🔧 Bildirim Sorunları - Çözüm Raporu

## ✅ Sorun 1: Aynı Bildirim Defalarca Geliyor

### Problem
Her 30 saniyede bir backend kontrolü yapılırken, aynı okunmamış bildirim tekrar tekrar gösteriliyordu.

### Çözüm
`BackgroundNotificationService`'e **bildirim takip sistemi** eklendi:

```typescript
private displayedNotificationIds: Set<number> = new Set();
```

**Çalışma Mantığı**:
1. Bir bildirim gösterildiğinde ID'si `Set`'e eklenir
2. Sonraki kontrollerde, zaten gösterilmiş bildirimler filtrelenir
3. Sadece **yeni** bildirimleri gösterir

**Değişiklikler**:
- ✅ `displayedNotificationIds` Set eklendi
- ✅ `checkForNewNotifications()` metodunda filtreleme
- ✅ `clearDisplayedNotifications()` metodu eklendi (kullanıcı bildirimleri okuduğunda çağrılabilir)
- ✅ `stop()` metodunda cleanup

**Sonuç**: Artık her bildirim sadece **bir kez** gösterilir! ✨

---

## ⚠️ Sorun 2: Uygulama Kapalıyken Bildirim Gelmiyor

### Problem
Uygulama tamamen kapalıyken backend'den gelen bildirimler telefona ulaşmıyor.

### Neden?
Backend'den **gerçek FCM push notification** gönderilmesi gerekiyor. Periyodik kontrol sadece uygulama açıkken çalışır.

### Kontrol Edildi
✅ Firebase service account dosyası mevcut: `D:\ElementElektrik\Mobile\backend\firebase-service-account.json`
✅ FirebaseAdmin paketi yüklü
✅ PushNotificationService kodu hazır

### Çözüm: Backend'i Çalıştırın ve Test Edin

#### Adım 1: Backend'i Başlatın

```cmd
cd D:\ElementElektrik\Mobile\backend
dotnet run
```

**Log'larda şunu arayın**:
```
Firebase Admin SDK initialized successfully
```

Eğer görmezseniz, ilk bildirim gönderildiğinde initialize olacak (lazy loading).

#### Adım 2: Mobil Uygulamayı Çalıştırın

```cmd
cd D:\ElementElektrik\Mobile\SantiyeTalepMobile
npx react-native start
# Yeni terminal
npx react-native run-android
```

#### Adım 3: Test Push Notification Gönderin

**Yöntem 1: API Test Endpoint** (Yeni Eklendi!)

Mobil uygulamaya login olduktan sonra:

```bash
# Postman veya curl ile
POST http://localhost:5136/api/notification/test-push
Authorization: Bearer YOUR_JWT_TOKEN
```

Veya mobil app'ten test butonu ekleyebilirsiniz.

**Yöntem 2: WebUI'dan Gerçek Talep**

1. Mobil uygulamaya login ol (tedarikçi olarak)
2. **Uygulamayı tamamen kapat** (Recent apps'ten kaydır)
3. WebUI'dan o tedarikçiye talep gönder
4. **📱 Anında bildirim gelecek!**

#### Adım 4: Backend Log'larını Kontrol Edin

Başarılı push notification:
```
Successfully sent FCM notification. Message ID: projects/element-elektrik/messages/xxxxx
```

Hata durumu:
```
[SIMULATED] FCM notification sent to token: ...
```
Bu durumda Firebase initialize olmamış demektir.

---

## 🧪 Test Senaryoları

### Test 1: Tekrarlayan Bildirim Sorunu

**Adımlar**:
1. Mobil uygulamayı aç
2. Backend'den bildirim gelsin (WebUI'dan talep gönder)
3. 30 saniye bekle
4. Bildirim tekrar gelmemeli ✅

**Beklenen**: Aynı bildirim sadece bir kez gösterilir.

---

### Test 2: Uygulama Kapalıyken Push Notification

**Adımlar**:
1. Backend çalışıyor olmalı (`dotnet run`)
2. Mobil uygulamaya login ol
3. Console'da "FCM token registered successfully" görmelisin
4. **Uygulamayı tamamen kapat**
5. WebUI'dan tedarikçiye talep gönder
6. **📱 Telefonda bildirim gelecek** (ses + titreşim)

**Beklenen**: 
- Bildirim anında gelir (1-2 saniye içinde)
- Notification tray'de görünür
- Ses çalar
- Telefon titrer

**Backend log'unda göreceksin**:
```
Firebase Admin SDK initialized successfully
Creating notification for supplier X (User: Y) for request Z
Successfully sent FCM notification. Message ID: ...
```

---

### Test 3: Uygulama Açıkken Bildirim

**Adımlar**:
1. Mobil uygulamayı aç
2. WebUI'dan talep gönder
3. Bildirim hem ekranda hem notification tray'de görünecek

**Beklenen**: Foreground notification + local notification

---

## 🔍 Sorun Giderme

### Backend'de "[SIMULATED]" Görüyorum

**Sorun**: Firebase initialize olmamış.

**Çözüm**:
```cmd
cd D:\ElementElektrik\Mobile\backend
dir firebase-service-account.json  # Dosya var mı?

# Dosya varsa, backend'i restart et
taskkill /F /IM dotnet.exe
dotnet run
```

### Mobil App'te "FCM Token" Görünmüyor

**Sorun**: Firebase mobil SDK çalışmıyor.

**Kontrol**:
- `google-services.json` dosyası `android/app/` dizininde mi?
- `@react-native-firebase/app` ve `@react-native-firebase/messaging` paketleri yüklü mü?

```cmd
cd SantiyeTalepMobile
npm list @react-native-firebase
```

### Database'de FcmToken Kolonu Boş

**Sorun**: Token backend'e kaydedilmemiş.

**Kontrol**:
```sql
SELECT Id, Email, Phone, FcmToken FROM Users WHERE Role = 'Supplier'
```

`FcmToken` kolonu dolu olmalı. Boşsa:
1. Mobil uygulamayı logout yap
2. Yeniden login ol
3. Console'da "FCM token registered successfully" kontrol et

---

## 📱 Mobil Uygulama Build

Değişiklikleri uygulamak için:

```cmd
cd D:\ElementElektrik\Mobile\SantiyeTalepMobile

# Android için
npx react-native run-android

# Veya manuel
cd android
.\gradlew assembleDebug
adb install -r app\build\outputs\apk\debug\app-debug.apk
```

---

## 📊 Beklenen Sonuçlar

### ✅ Sorun 1 Düzeltildikten Sonra
- Aynı bildirim sadece **bir kez** gösterilir
- Her 30 saniyede tekrar gelmez
- Sadece **yeni** bildirimleri gösterir

### ✅ Sorun 2 Düzeltildikten Sonra
| Durum | Sonuç |
|-------|-------|
| Uygulama açık | ✅ Anında bildirim |
| Uygulama arka plan | ✅ Anında bildirim |
| **Uygulama kapalı** | ✅ **Anında bildirim** (FCM push) |
| Ses | ✅ Çalar |
| Titreşim | ✅ Aktif |

---

## 🚀 Hızlı Test Komutu

**Terminal 1 - Backend**:
```cmd
cd D:\ElementElektrik\Mobile\backend
dotnet run
```

**Terminal 2 - Mobile App**:
```cmd
cd D:\ElementElektrik\Mobile\SantiyeTalepMobile
npx react-native run-android
```

**Test**:
1. Login ol
2. Uygulamayı kapat
3. WebUI'dan talep gönder
4. **📱 Bildirim gelecek!**

---

## ✅ Checklist

Backend:
- [ ] Firebase service account dosyası mevcut
- [ ] Backend başlatıldı (`dotnet run`)
- [ ] Log'da "Firebase Admin SDK initialized" görüldü

Mobile App:
- [ ] Uygulama build edildi (yeni değişikliklerle)
- [ ] Login olundu
- [ ] FCM token kaydedildi
- [ ] Database'de FcmToken kolonu dolu

Test:
- [ ] Uygulama açıkken bildirim geldi
- [ ] Uygulama kapalıyken bildirim geldi (**EN ÖNEMLİ**)
- [ ] Aynı bildirim tekrar gelmedi
- [ ] Ses + titreşim çalıştı

---

## 🎯 Özet

**Sorun 1**: ✅ Çözüldü - Tekrarlayan bildirimler engellendi
**Sorun 2**: ⚠️ Backend çalıştırıp test edilmeli - Firebase hazır

**Sonraki adım**: Backend'i başlat ve test et!
