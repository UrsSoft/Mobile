using SantiyeTalepApi.Models;
using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.DTOs
{
    public class RequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int SiteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public Unit Unit { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public RequestCategory Category { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public EmployeeDto Employee { get; set; } = null!;
        public SiteDto Site { get; set; } = null!;
        public List<OfferDto> Offers { get; set; } = new List<OfferDto>();
    }

    public class CreateRequestDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [MinLength(3, ErrorMessage = "Ürün açıklaması en az 3 karakter olmalıdır")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required]
        public Unit Unit { get; set; }

        [Required]
        public DeliveryType DeliveryType { get; set; }

        [Required]
        public RequestCategory Category { get; set; }

        public DateTime? RequiredDate { get; set; }
    }

    public class OfferDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int SupplierId { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DeliveryDays { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime OfferDate { get; set; }
        public SupplierDto Supplier { get; set; } = null!;
    }

    public class CreateOfferDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal Price { get; set; }

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Teslimat süresi en az 1 gün olmalıdır")]
        public int DeliveryDays { get; set; }
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

    public class ExternalProductDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public string? Category { get; set; }
        public List<string>? Units { get; set; }
        public string? Title { get; set; } // Alternative to Name
        public string? ProductName { get; set; } // Alternative to Name
        public string? BrandName { get; set; } // Alternative to Brand
        public string? Type { get; set; } // Alternative to Category
        public string? Unit { get; set; } // Single unit as string
    }
}
