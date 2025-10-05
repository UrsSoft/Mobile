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

        [Required(ErrorMessage = "Teslimat tipi seçimi gereklidir")]
        public DeliveryType DeliveryType { get; set; }

        public string? Description { get; set; }
    }

    public class RequestDto
    {
        public int Id { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public decimal? EstimatedCost { get; set; }
        public List<OfferDto> Offers { get; set; } = new();
    }

    public class EmployeeRequestDto
    {
        public int Id { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public List<OfferDto> Offers { get; set; } = new();
        public int OfferCount => Offers?.Count ?? 0;
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
        public int TotalRequests { get; set; }
        public int OpenRequests { get; set; }
        public int CompletedRequests { get; set; }
    }
}