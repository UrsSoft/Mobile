using System.ComponentModel.DataAnnotations;
using SantiyeTalepWebUI.Models.DTOs;

namespace SantiyeTalepWebUI.Models.ViewModels
{
    public class CreateRequestViewModel
    {
        [Required(ErrorMessage = "Ürün açıklaması gereklidir")]
        [MinLength(3, ErrorMessage = "Ürün açıklaması en az 3 karakter olmalıdır")]
        [Display(Name = "Ürün Açıklaması")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        [Display(Name = "Miktar")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Teslim tipi seçimi gereklidir")]
        [Display(Name = "Teslim Tipi")]
        public DeliveryType DeliveryType { get; set; }
                
        [Display(Name = "Açıklama")]
        public string? Description { get; set; } = string.Empty;
    }

    public class RequestDetailsViewModel
    {
        public RequestDto Request { get; set; } = null!;
        public List<OfferDto> Offers { get; set; } = new();
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
    }

    public class ProductSearchViewModel
    {
        [Required]
        [MinLength(2, ErrorMessage = "Arama terimi en az 2 karakter olmalıdır")]
        [Display(Name = "Ürün Ara")]
        public string SearchTerm { get; set; } = string.Empty;
        
        public List<ProductDto> SearchResults { get; set; } = new();
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductSearchDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Arama terimi en az 2 karakter olmalıdır")]
        public string SearchTerm { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public static class EnumDisplayHelper
    {
        public static string GetDisplayName(this DeliveryType deliveryType)
        {
            return deliveryType switch
            {
                DeliveryType.TodayPickup => "Bugün araç gönderip aldıracağım",
                DeliveryType.SameDayDelivery => "Gün içi siz sevk edin",
                DeliveryType.NextDayDelivery => "Yarın siz sevk edin",
                DeliveryType.BusinessDays1to2 => "1-2 iş günü",
                _ => deliveryType.ToString()
            };
        }
    }
}