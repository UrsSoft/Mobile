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
}
