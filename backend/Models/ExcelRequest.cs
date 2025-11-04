using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SantiyeTalepApi.Models
{
    /// <summary>
    /// Excel ile talep yönetimi için model
    /// </summary>
    public class ExcelRequest
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Ýlgili þantiye ID
        /// </summary>
        [Required]
        public int SiteId { get; set; }

        [ForeignKey(nameof(SiteId))]
        public virtual Site? Site { get; set; }

        /// <summary>
        /// Talep eden çalýþan ID
        /// </summary>
        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        /// <summary>
        /// Admin tarafýndan yüklenen orijinal Excel dosya adý
        /// </summary>
        [Required]
        [StringLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Sistemde saklanan dosya adý (unique)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// Dosya yolu
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Dosya boyutu (bytes)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Yükleme tarihi
        /// </summary>
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Durum
        /// </summary>
        public ExcelRequestStatus Status { get; set; } = ExcelRequestStatus.Uploaded;

        /// <summary>
        /// Açýklama
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Excel talebe atanan tedarikçiler
        /// </summary>
        public virtual ICollection<ExcelRequestSupplier> AssignedSuppliers { get; set; } = new List<ExcelRequestSupplier>();

        /// <summary>
        /// Tedarikçiler tarafýndan yüklenen teklif dosyalarý
        /// </summary>
        public virtual ICollection<SupplierExcelOffer> SupplierOffers { get; set; } = new List<SupplierExcelOffer>();
    }

    /// <summary>
    /// Excel talebe atanan tedarikçi iliþkisi
    /// </summary>
    public class ExcelRequestSupplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExcelRequestId { get; set; }

        [ForeignKey(nameof(ExcelRequestId))]
        public virtual ExcelRequest? ExcelRequest { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        /// <summary>
        /// Atanma tarihi
        /// </summary>
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Tedarikçi dosyayý indirdi mi?
        /// </summary>
        public bool Downloaded { get; set; } = false;

        /// <summary>
        /// Ýndirme tarihi
        /// </summary>
        public DateTime? DownloadedDate { get; set; }

        /// <summary>
        /// Tedarikçi teklif dosyasý yükledi mi?
        /// </summary>
        public bool OfferUploaded { get; set; } = false;

        /// <summary>
        /// Teklif yükleme tarihi
        /// </summary>
        public DateTime? OfferUploadedDate { get; set; }
    }

    /// <summary>
    /// Tedarikçi tarafýndan yüklenen teklif dosyasý
    /// </summary>
    public class SupplierExcelOffer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExcelRequestId { get; set; }

        [ForeignKey(nameof(ExcelRequestId))]
        public virtual ExcelRequest? ExcelRequest { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        /// <summary>
        /// Tedarikçi tarafýndan yüklenen dosya adý
        /// </summary>
        [Required]
        [StringLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Sistemde saklanan dosya adý
        /// </summary>
        [Required]
        [StringLength(500)]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// Dosya yolu
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Dosya boyutu
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Yükleme tarihi
        /// </summary>
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Durum
        /// </summary>
        public OfferExcelStatus Status { get; set; } = OfferExcelStatus.Submitted;

        /// <summary>
        /// Admin onayý
        /// </summary>
        public bool ApprovedByAdmin { get; set; } = false;

        /// <summary>
        /// Onay tarihi
        /// </summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>
        /// Açýklama/Not
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
