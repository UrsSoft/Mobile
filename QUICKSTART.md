# Şantiye Talep Sistemi - Hızlı Başlangıç

Bu rehber, tüm sistem bileşenlerini hızlı bir şekilde çalıştırmanıza yardımcı olacaktır.

## Sistem Gereksinimleri

- .NET 8 SDK
- Node.js 16+
- SQL Server LocalDB (Windows) veya SQL Server

## Adım Adım Kurulum

### 1. Backend'i Başlatın

```bash
# Backend dizinine gidin
cd backend

# Bağımlılıkları yükleyin
dotnet restore

# Veritabanını oluşturun
dotnet ef database update

# API'yi başlatın
dotnet run
```

Backend çalıştığında:
- API: `http://localhost:5136`
- Swagger UI: `http://localhost:5136/swagger`

### 2. Web Arayüzünü Başlatın

```bash
# Yeni terminal açın
# Web frontend dizinine gidin
cd web-frontend

# Bağımlılıkları yükleyin
npm install

# Development server'ı başlatın
npm run dev
```

Web arayüzü çalıştığında:
- Web App: `http://localhost:5173`

### 3. Mobil Uygulamayı Başlatın (Opsiyonel)

```bash
# Yeni terminal açın
# Mobil uygulama dizinine gidin
cd SantiyeTalepMobile

# Bağımlılıkları yükleyin
npm install

# Android için (Android Studio gerekli)
npx react-native run-android

# iOS için (macOS + Xcode gerekli)
cd ios && pod install && cd ..
npx react-native run-ios
```

## Test Hesapları

### Web Arayüzü için:

1. **Admin Hesabı**
   - Email: `admin@test.com`
   - Şifre: `admin123`
   - Özellikler: Tüm sistem yönetimi

2. **Çalışan Hesabı**
   - Email: `employee@test.com`
   - Şifre: `employee123`
   - Özellikler: Talep oluşturma ve takip

3. **Tedarikçi Hesabı**
   - Email: `supplier@test.com`
   - Şifre: `supplier123`
   - Özellikler: Teklif verme

### Tedarikçi Kaydı

Yeni tedarikçi hesabı oluşturmak için:
1. Web arayüzünde `http://localhost:5173/register` adresine gidin
2. Tedarikçi bilgilerini doldurun
3. Admin hesabıyla giriş yapıp tedarikçiyi onaylayın

## Hızlı Test Senaryosu

1. **Admin ile giriş yapın**
   - `http://localhost:5173/login` adresine gidin
   - admin@test.com / admin123 ile giriş yapın
   - Admin panelinden sistem durumunu kontrol edin

2. **Şantiye oluşturun**
   - Admin Panel > Şantiye Yönetimi
   - "Yeni Şantiye" butonuna tıklayın
   - Şantiye bilgilerini doldurun

3. **Çalışan hesabıyla talep oluşturun**
   - Çıkış yapın, employee@test.com ile giriş yapın
   - "Talepler" > "Yeni Talep" (özellik yakında)

4. **Tedarikçi hesabıyla teklif verin**
   - Çıkış yapın, supplier@test.com ile giriş yapın
   - Mevcut talepleri görüntüleyin ve teklif verin

## Sorun Giderme

### Backend Çalışmıyor
- SQL Server LocalDB kurulu olduğundan emin olun
- Connection string'i `appsettings.json`'da kontrol edin
- `dotnet ef database update` komutunu tekrar çalıştırın

### Web Frontend Çalışmıyor
- Node.js versiyonunu kontrol edin (16+)
- `npm install` komutunu tekrar çalıştırın
- Browser'da `http://localhost:5173` adresini açın

### API Bağlantı Hatası
- Backend'in `http://localhost:5136` adresinde çalıştığından emin olun
- CORS ayarlarını kontrol edin
- Browser developer tools'da network sekmesini inceleyin

## Önemli Dosyalar

- Backend config: `backend/appsettings.json`
- Web API base URL: `web-frontend/src/services/api.ts`
- Database context: `backend/Data/ApplicationDbContext.cs`

## Sonraki Adımlar

Sistem çalıştığında:
1. Admin panelinden tedarikçi onaylarını kontrol edin
2. Şantiye ve çalışan yönetimini test edin
3. Talep ve teklif süreçlerini deneyin
4. Profil yönetimi özelliklerini keşfedin

## Yardım

Sorunlarla karşılaştığınızda:
- Console ve browser dev tools loglarını kontrol edin
- README dosyalarını inceleyin
- API dokumentasyonu için Swagger UI'ı kullanın
