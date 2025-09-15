using System.ComponentModel.DataAnnotations;
using SantiyeTalepWebUI.Models.DTOs;

namespace SantiyeTalepWebUI.Models.ViewModels
{
    public class CreateRequestViewModel
    {
        [Required(ErrorMessage = "Başlık gereklidir")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama gereklidir")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ürün açıklaması gereklidir")]
        [MinLength(3, ErrorMessage = "Ürün açıklaması en az 3 karakter olmalıdır")]
        [Display(Name = "Ürün Açıklaması")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birim seçimi gereklidir")]
        [Display(Name = "Birim")]
        public Unit Unit { get; set; }

        [Required(ErrorMessage = "Teslim tipi seçimi gereklidir")]
        [Display(Name = "Teslim Tipi")]
        public DeliveryType DeliveryType { get; set; }

        [Required(ErrorMessage = "Kategori seçimi gereklidir")]
        [Display(Name = "Kategori")]
        public RequestCategory Category { get; set; }

        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        [Display(Name = "Miktar")]
        public int Quantity { get; set; }

        [Display(Name = "Gerekli Tarih")]
        public DateTime RequiredDate { get; set; } = DateTime.Now.AddDays(7);
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

    public static class EnumDisplayHelper
    {
        public static string GetDisplayName(this Unit unit)
        {
            return unit switch
            {
                Unit.Adet => "Adet",
                Unit.Kilogram => "Kilogram",
                Unit.Metre => "Metre",
                _ => unit.ToString()
            };
        }

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

        public static string GetDisplayName(this RequestCategory category)
        {
            return category switch
            {
                RequestCategory.Material => "Malzeme",
                RequestCategory.Service => "Hizmet",
                RequestCategory.Equipment => "Ekipman",
                RequestCategory.Other => "Diğer",
                _ => category.ToString()
            };
        }
    }
}