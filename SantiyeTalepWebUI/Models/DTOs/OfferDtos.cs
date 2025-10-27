using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class CreateOfferDto
    {
        [Required(ErrorMessage = "Talep ID gereklidir")]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Marka gereklidir")]
        [MinLength(2, ErrorMessage = "Marka en az 2 karakter olmalıdır")]
        public string Brand { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama gereklidir")]
        [MinLength(10, ErrorMessage = "Açıklama en az 10 karakter olmalıdır")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Fiyat gereklidir")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Para birimi seçimi gereklidir")]
        public Currency Currency { get; set; } = Currency.TRY;

        [Range(0, 100, ErrorMessage = "İskonto 0-100 arasında olmalıdır")]
        public decimal Discount { get; set; } = 0;

        [Required(ErrorMessage = "Teslimat tipi seçimi gereklidir")]
        public DeliveryType DeliveryType { get; set; }

        public DateTime DeliveryDate { get; set; } = DateTime.Now.AddDays(14);

        // Hesaplanan değerler
        public decimal TotalPrice => Price * Quantity;
        public decimal DiscountAmount => (TotalPrice * Discount) / 100;
        public decimal FinalPrice => TotalPrice - DiscountAmount;
    }

    public class BulkOfferRowDto
    {
        public int RequestId { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Currency Currency { get; set; } = Currency.TRY;
        public decimal Discount { get; set; } = 0;
        public DeliveryType DeliveryType { get; set; }
    }

    public class BulkCreateOfferDto
    {
        public List<BulkOfferRowDto> Offers { get; set; } = new List<BulkOfferRowDto>();
    }

    public class OfferDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string RequestTitle { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierEmail { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Currency Currency { get; set; }
        public decimal Discount { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public int DeliveryDays { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime OfferDate { get; set; }
        
        // Hesaplanan değerler
        public decimal TotalPrice => Price * Quantity;
        public decimal DiscountAmount => (TotalPrice * Discount) / 100;
        public decimal FinalPrice => TotalPrice - DiscountAmount;
        public string DeliveryTypeText => DeliveryType switch
        {
            DeliveryType.TodayPickup => "Bugün araç gönderip aldıracağım",
            DeliveryType.SameDayDelivery => "Gün içi siz sevk edin",
            DeliveryType.NextDayDelivery => "Yarın siz sevk edin",
            DeliveryType.BusinessDays1to2 => "1-2 iş günü",
            _ => "Bilinmiyor"
        };
        
        public string CurrencySymbol => Currency switch
        {
            Currency.TRY => "₺",
            Currency.USD => "$",
            Currency.EUR => "€",
            Currency.GBP => "£",
            _ => "₺"
        };
        
        public string CurrencyName => Currency switch
        {
            Currency.TRY => "Türk Lirası",
            Currency.USD => "Amerikan Doları",
            Currency.EUR => "Euro",
            Currency.GBP => "İngiliz Sterlini",
            _ => "Türk Lirası"
        };
    }

    public class OfferListViewModel
    {
        public List<OfferDto> Offers { get; set; } = new();
        public OfferStatus? StatusFilter { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}