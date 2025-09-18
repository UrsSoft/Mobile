using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class CreateOfferDto
    {
        [Required(ErrorMessage = "Talep ID gereklidir")]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Fiyat gereklidir")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal Price { get; set; }

        public string Description { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; } = DateTime.Now.AddDays(14);
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
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DeliveryDays { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime OfferDate { get; set; }
    }

    public class OfferListViewModel
    {
        public List<OfferDto> Offers { get; set; } = new();
        public OfferStatus? StatusFilter { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}