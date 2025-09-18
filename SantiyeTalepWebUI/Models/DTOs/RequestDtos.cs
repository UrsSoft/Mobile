using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class CreateRequestDto
    {
        [Required(ErrorMessage = "Ürün açıklaması gereklidir")]
        [MinLength(3, ErrorMessage = "Ürün açıklaması en az 3 karakter olmalıdır")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Teslim tipi seçimi gereklidir")]
        public DeliveryType DeliveryType { get; set; }

        [Required(ErrorMessage = "Açıklama gereklidir")]
        public string Description { get; set; } = string.Empty;
    }

    // DTO for admin users - includes offers
    public class RequestDto
    {
        public int Id { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public string Description { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal? EstimatedCost { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public List<OfferDto> Offers { get; set; } = new();
    }

    // DTO for employee users - excludes offers for security
    public class EmployeeRequestDto
    {
        public int Id { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public string Description { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        // Offers are intentionally excluded - only admins can see offers
        public int OfferCount { get; set; } // Only show count for employees
    }

    public class RequestListViewModel
    {
        public List<RequestDto> Requests { get; set; } = new();
        public RequestStatus? StatusFilter { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class EmployeeRequestListViewModel
    {
        public List<EmployeeRequestDto> Requests { get; set; } = new();
        public RequestStatus? StatusFilter { get; set; }
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