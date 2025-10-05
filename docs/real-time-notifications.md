# Real-time Notification System

Bu özellik, Admin ve Tedarikçi panellerinde yeni taleplerin 60 saniye de bir kontrol edilmesini ve bildirim gösterilmesini saðlar.

## Özellikler

### ?? Otomatik Kontrol
- Her 60 saniyede bir yeni talepler kontrol edilir
- Sayfa görünür deðilse kontrol durdurulur (performans için)
- Sayfa tekrar görünür olduðunda kontrol devam eder

### ?? Bildirim Türleri

#### Tedarikçi için:
- Yeni açýk talepler
- Okunmamýþ bildirimler
- Teklif durumu güncellemeleri

#### Admin için:
- Günlük yeni talepler
- Bekleyen tedarikçi onaylarý
- Bekleyen teklif onaylarý

### ?? Görsel Özellikler
- Toast bildirimleri (sað üst köþe)
- Ses bildirimi (opsiyonel)
- Badge animasyonlarý
- Canlý takip durumu göstergesi

### ??? Teknik Detaylar

#### Dosyalar:
- `~/js/notification-checker.js` - Ana bildirim sistemi
- `~/css/notification-checker.css` - Bildirim stilleri
- API endpoints:
  - `/Supplier/CheckNewRequests` - Tedarikçi için kontrol
  - `/Admin/CheckNewRequests` - Admin için kontrol

#### API Yanýtlarý:

**Tedarikçi:**
```json
{
  "success": true,
  "data": {
    "newRequestCount": 5,
    "unreadNotificationCount": 3,
    "hasNewContent": true
  }
}
```

**Admin:**
```json
{
  "success": true,
  "data": {
    "newRequestsToday": 8,
    "pendingSuppliersCount": 2,
    "pendingOffersCount": 12,
    "hasNewContent": true
  }
}
```

### ?? Kullaným

Sistem otomatik olarak þu sayfalarda etkinleþir:
- `/Admin/Dashboard`
- `/Supplier/Dashboard`
- `data-enable-notifications="true"` özelliðine sahip sayfalar

### ?? Yapýlandýrma

```javascript
// Manuel baþlatma
window.notificationChecker = new NotificationChecker({
    userRole: 'admin', // 'admin' veya 'supplier'
    checkUrl: '/Admin/CheckNewRequests',
    checkInterval: 60000, // 60 saniye
    enableSound: true // Ses bildirimi
});
```

### ?? Özelleþtirme

#### Kontrol süresini deðiþtirme:
```javascript
notificationChecker.setInterval(30000); // 30 saniye
```

#### Sesi kapatma:
```javascript
notificationChecker.notificationSound = false;
```

#### Manuel kontrol:
```javascript
notificationChecker.manualCheck();
```

### ?? Responsive Tasarým
- Mobil cihazlarda uyumlu
- Farklý ekran boyutlarý için optimize
- Touch-friendly arayüz

### ?? Sorun Giderme

#### Bildirimler çalýþmýyor:
1. Console'da JavaScript hatalarý kontrol edin
2. API endpoint'lerinin çalýþtýðýndan emin olun
3. Sayfa body'sine doðru class eklendiðini kontrol edin

#### Performans sorunlarý:
- Interval süresini artýrýn (60s -> 120s)
- Ses bildirimlerini kapatýn
- Sayfa görünürlük kontrolünün çalýþtýðýndan emin olun

### ?? Gelecek Geliþtirmeler
- [ ] Push notification desteði
- [ ] WebSocket ile gerçek zamanlý bildirimler
- [ ] Bildirim geçmiþi
- [ ] Kullanýcý özel bildirim ayarlarý
- [ ] Email/SMS entegrasyonu