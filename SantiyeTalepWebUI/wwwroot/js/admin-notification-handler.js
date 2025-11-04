// Admin Dashboard Notification Handler
// Bu dosya admin bildirimleri için yönlendirme işlemlerini yapar

function goToRelatedPage(type, requestId, offerId, supplierId, notificationId) {
    // Önce bildirimi okundu olarak işaretle (eğer okunmamışsa)
    const notification = notifications.find(n => n.id === notificationId);
    if (notification && !notification.isRead) {
        // Async olarak okundu işaretle ama sayfaya yönlendirmeyi bekletme
        fetch('/Admin/MarkNotificationAsRead', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(notificationId)
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Lokal olarak güncelle
                notification.isRead = true;
                displayNotifications();
                loadNotificationSummary();
            }
        })
        .catch(error => {
            console.error('Bildirim güncellenirken hata:', error);
        });
    }

    // Sayfaya yönlendir
    let targetUrl = '/Admin/Dashboard';

    switch(type) {
        case 1: // SupplierRegistration
            targetUrl = '/Admin/Suppliers';
            break;
        case 2: // NewRequest - Talep bildirimi ise Requests sayfasına git
            targetUrl = '/Admin/Requests';
            if (requestId && requestId !== 'null') {
                targetUrl += `?requestId=${requestId}`;
            }
            break;
        case 3: // NewOffer - Teklif bildirimi ise Offers sayfasına git
            targetUrl = '/Admin/Offers';
            if (offerId && offerId !== 'null') {
                targetUrl += `?offerId=${offerId}`;
            }
            break;
        case 11: // ExcelRequestAssigned - Excel talebi atandığında Excel yönetim sayfasına git
            targetUrl = '/Admin/ExcelManagement';
            if (requestId && requestId !== 'null') {
                targetUrl += `?requestId=${requestId}`;
            }
            break;
        case 12: // ExcelOfferUploaded - Tedarikçi Excel teklifi yüklediğinde Excel yönetim sayfasına git
            targetUrl = '/Admin/ExcelManagement';
            if (requestId && requestId !== 'null') {
                targetUrl += `?requestId=${requestId}`;
            }
            break;
        default:
            targetUrl = '/Admin/Dashboard';
            break;
    }

    // Kısa bir gecikme ile sayfaya yönlendir (okundu işaretleme tamamlansın diye)
    setTimeout(() => {
        window.location.href = targetUrl;
    }, 100);
}
