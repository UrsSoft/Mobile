using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class CreateRequestDto
    {
        [Required(ErrorMessage = "Başlık gereklidir")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama gereklidir")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ürün açıklaması gereklidir")]
        [MinLength(3, ErrorMessage = "Ürün açıklaması en az 3 karakter olmalıdır")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birim seçimi gereklidir")]
        public Unit Unit { get; set; }

        [Required(ErrorMessage = "Teslim tipi seçimi gereklidir")]
        public DeliveryType DeliveryType { get; set; }

        [Required(ErrorMessage = "Kategori seçimi gereklidir")]
        public RequestCategory Category { get; set; }

        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        public int Quantity { get; set; }

        public DateTime RequiredDate { get; set; } = DateTime.Now.AddDays(7);
    }

    public class RequestDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public Unit Unit { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public RequestCategory Category { get; set; }
        public int Quantity { get; set; }
        public DateTime RequiredDate { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public List<OfferDto> Offers { get; set; } = new();
    }

    public class RequestListViewModel
    {
        public List<RequestDto> Requests { get; set; } = new();
        public RequestStatus? StatusFilter { get; set; }
        public RequestCategory? CategoryFilter { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class ProductSearchDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Arama terimi en az 2 karakter olmalıdır")]
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Units { get; set; } = new List<string>();
    }
}