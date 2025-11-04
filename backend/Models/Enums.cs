namespace SantiyeTalepApi.Models
{
    public enum UserRole
    {
        Admin = 1,
        Employee = 2,
        Supplier = 3
    }

    public enum SupplierStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum RequestStatus
    {
        Open = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum OfferStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum RequestCategory
    {
        Material = 1,
        Service = 2,
        Equipment = 3,
        Other = 4
    }

    public enum Unit
    {
        Adet = 1,
        Kilogram = 2,
        Metre = 3
    }

    public enum DeliveryType
    {
        TodayPickup = 1, // Bugün araç gönderip aldıracağım
        SameDayDelivery = 2, // Gün içi siz sevk edin
        NextDayDelivery = 3, // Yarın siz sevk edin
        BusinessDays1to2 = 4 // 1-2 iş günü
    }

    public enum Currency
    {
        TRY = 1, // Türk Lirası
        USD = 2, // Amerikan Doları
        EUR = 3, // Euro
        GBP = 4  // İngiliz Sterlini
    }

    public enum NotificationType
    {
        SupplierRegistration = 1, // Tedarikçi kayıt olduğunda
        NewRequest = 2, // Çalışan talep girdiğinde
        NewOffer = 3, // Tedarikçi teklif verdiğinde
        RequestApproved = 4, // Talep onaylandığında
        RequestRejected = 5, // Talep reddedildiğinde
        OfferApproved = 6, // Teklif onaylandığında
        OfferRejected = 7, // Teklif reddedildiğinde
        SupplierApproved = 8, // Tedarikçi onaylandığında
        SupplierRejected = 9, // Tedarikçi reddedildiğinde
        RequestSentToSupplier = 10, // Admin talebi tedarikçilere gönderdiğinde
        ExcelRequestAssigned = 11, // Excel talebi tedarikçiye atandığında
        ExcelOfferUploaded = 12 // Tedarikçi Excel teklifi yüklediğinde
    }

    /// <summary>
    /// Excel talep durumları
    /// </summary>
    public enum ExcelRequestStatus
    {
        Uploaded = 0,           // Yüklendi
        AssignedToSuppliers = 1, // Tedarikçilere atandı
        InProgress = 2,         // İşlemde (bazı tedarikçiler teklif yükledi)
        Completed = 3,          // Tamamlandı (tüm tedarikçiler teklif yükledi)
        Cancelled = 4           // İptal edildi
    }

    /// <summary>
    /// Tedarikçi teklif Excel durumları
    /// </summary>
    public enum OfferExcelStatus
    {
        Submitted = 0,          // Gönderildi
        UnderReview = 1,        // İnceleniyor
        Approved = 2,           // Onaylandı
        Rejected = 3            // Reddedildi
    }
}
