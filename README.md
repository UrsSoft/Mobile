# Şantiye Talep Yönetim Sistemi

Şantiye çalışanlarının malzeme/hizmet taleplerini yönetmek, tedarikçilerden teklif almak ve onay süreçlerini dijitalleştirmek için geliştirilmiş full-stack platform.

## Proje Yapısı

```
Mobile/
├── backend/                 # .NET Web API Backend
│   ├── Controllers/         # API Controllers
│   ├── Models/             # Veri modelleri
│   ├── Data/               # Entity Framework DbContext
│   ├── DTOs/               # Data Transfer Objects
│   └── Services/           # Business Logic
├── SantiyeTalepMobile/     # React Native Mobil Uygulama
│   ├── src/
│   │   ├── screens/        # Ekranlar
│   │   ├── components/     # Reusable componentler
│   │   ├── navigation/     # Navigation yapısı
│   │   ├── services/       # API servisleri
│   │   ├── context/        # React Context (Auth)
│   │   └── types/          # TypeScript tip tanımları
│   └── android/           # Android native dosyalar
└── docs/                  # Proje dokümantasyonu
```

## Teknoloji Stack

### Backend (.NET)
- **Framework:** .NET 8 Web API
- **Veritabanı:** SQL Server (LocalDB)
- **ORM:** Entity Framework Core
- **Authentication:** JWT Bearer Token
- **Documentation:** Swagger/OpenAPI
- **Packages:**
  - Microsoft.EntityFrameworkCore.SqlServer
  - Microsoft.AspNetCore.Authentication.JwtBearer
  - AutoMapper
  - BCrypt.Net-Next

### Frontend (React Native)
- **Framework:** React Native 0.81+
- **Language:** TypeScript
- **Navigation:** React Navigation v6
- **HTTP Client:** Axios
- **State Management:** React Context API
- **Storage:** AsyncStorage
- **Packages:**
  - @react-navigation/native
  - @react-navigation/stack
  - @react-navigation/bottom-tabs
  - react-native-gesture-handler
  - react-native-screens

## Sistem Rolleri

### 1. Yönetici (Admin)
- Çalışan ekleme/düzenleme/silme
- Şantiye ekleme/düzenleme/silme
- Tedarikçi onaylama/reddetme
- Teklif onaylama/reddetme
- Sistem geneli raporlama

### 2. Çalışan (Employee)
- Talep oluşturma
- Kendi taleplerini görüntüleme
- Talep durumu takibi
- Profil güncelleme

### 3. Tedarikçi (Supplier)
- Sistem kaydı (onay bekler)
- Açık talepleri görüntüleme
- Teklif verme
- Teklif geçmişi görüntüleme

## Kurulum

### Backend Kurulumu

1. **Gereksinimler:**
   - .NET 8 SDK
   - SQL Server veya SQL Server LocalDB

2. **Kurulum:**
   ```bash
   cd backend
   dotnet restore
   dotnet build
   ```

3. **Veritabanı:**
   ```bash
   dotnet ef database update
   ```

4. **Çalıştırma:**
   ```bash
   dotnet run
   ```
   API: `https://localhost:7000`
   Swagger: `https://localhost:7000`

### React Native Kurulumu

1. **Gereksinimler:**
   - Node.js 16+
   - React Native CLI
   - Android Studio (Android için)
   - Xcode (iOS için)

2. **Kurulum:**
   ```bash
   cd SantiyeTalepMobile
   npm install
   ```

3. **Android Çalıştırma:**
   ```bash
   npx react-native run-android
   ```

4. **iOS Çalıştırma:**
   ```bash
   cd ios && pod install && cd ..
   npx react-native run-ios
   ```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Kullanıcı girişi
- `POST /api/auth/register-supplier` - Tedarikçi kaydı
- `POST /api/auth/logout` - Çıkış

### Admin (Sadece Admin)
- `POST /api/admin/employees` - Çalışan oluştur
- `POST /api/admin/sites` - Şantiye oluştur
- `PUT /api/admin/suppliers/{id}/approve` - Tedarikçi onayla
- `PUT /api/admin/offers/{id}/approve` - Teklif onayla

### Requests
- `GET /api/request` - Talepleri listele
- `POST /api/request` - Yeni talep oluştur (Çalışan)
- `GET /api/request/{id}` - Talep detayı
- `PUT /api/request/{id}/cancel` - Talebi iptal et

### Offers
- `GET /api/offer/request/{requestId}` - Talebe ait teklifler
- `POST /api/offer` - Teklif ver (Tedarikçi)
- `GET /api/offer/my-offers` - Kendi tekliflerim
- `PUT /api/offer/{id}/withdraw` - Teklifi geri çek

## Demo Hesaplar

### Admin
- **Email:** admin@santiye.com
- **Şifre:** admin123

### Test Senaryosu
1. Admin hesabıyla giriş yapın
2. Yeni şantiye oluşturun
3. Şantiyeye çalışan atayın
4. Tedarikçi kaydı yapın ve onaylayın
5. Çalışan hesabıyla talep oluşturun
6. Tedarikçi hesabıyla teklif verin
7. Admin hesabıyla teklifi onaylayın

## Özellikler

### Mevcut Özellikler
- ✅ JWT Authentication
- ✅ Role-based Authorization
- ✅ Kullanıcı yönetimi
- ✅ Talep oluşturma ve yönetimi
- ✅ Teklif sistemi
- ✅ Responsive mobil arayüz
- ✅ Swagger API dokümantasyonu

### Planlanan Özellikler
- 📋 Push notifications
- 📋 Dosya yükleme (resim, PDF)
- 📋 QR kod desteği
- 📋 Lokasyon servisleri
- 📋 Offline desteği
- 📋 Raporlama modülü
- 📋 Email bildirimleri

## Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit edin (`git commit -m 'Add amazing feature'`)
4. Push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## İletişim

Proje hakkında sorularınız için:
- GitHub Issues kullanın
- Email: your-email@example.com
