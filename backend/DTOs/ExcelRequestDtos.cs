using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.DTOs
{
    /// <summary>
    /// Excel talep oluþturma DTO
    /// </summary>
    public class CreateExcelRequestDto
    {
        [Required(ErrorMessage = "Þantiye seçimi zorunludur")]
        public int SiteId { get; set; }

        [Required(ErrorMessage = "Çalýþan seçimi zorunludur")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "En az bir tedarikçi seçmelisiniz")]
        [MinLength(1, ErrorMessage = "En az bir tedarikçi seçmelisiniz")]
        public List<int> SupplierIds { get; set; } = new();

        [StringLength(1000)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Excel talep detay DTO
    /// </summary>
    public class ExcelRequestDto
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public string? Description { get; set; }
        public List<AssignedSupplierDto> AssignedSuppliers { get; set; } = new();
        public List<SupplierOfferDto> SupplierOffers { get; set; } = new();
    }

    /// <summary>
    /// Atanan tedarikçi DTO
    /// </summary>
    public class AssignedSupplierDto
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public bool Downloaded { get; set; }
        public DateTime? DownloadedDate { get; set; }
        public bool OfferUploaded { get; set; }
        public DateTime? OfferUploadedDate { get; set; }
    }

    /// <summary>
    /// Tedarikçi teklif Excel DTO
    /// </summary>
    public class SupplierOfferDto
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public bool ApprovedByAdmin { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Tedarikçi için Excel talep listesi DTO
    /// </summary>
    public class SupplierExcelRequestDto
    {
        public int Id { get; set; }
        public int ExcelRequestId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime AssignedDate { get; set; }
        public bool Downloaded { get; set; }
        public DateTime? DownloadedDate { get; set; }
        public bool OfferUploaded { get; set; }
        public DateTime? OfferUploadedDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tedarikçi teklif yükleme DTO
    /// </summary>
    public class UploadSupplierOfferDto
    {
        [Required(ErrorMessage = "Excel talep ID gereklidir")]
        public int ExcelRequestId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
