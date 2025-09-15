using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SantiyeTalepApi.Models
{
    public class Request
    {
        public int Id { get; set; }
        
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        public int SiteId { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string ProductDescription { get; set; } = string.Empty; // Açıklama (ürün arama için)
        
        [Required]
        public Unit Unit { get; set; } // Birim
        
        [Required]
        public DeliveryType DeliveryType { get; set; } // Teslim Tipi
        
        public RequestCategory Category { get; set; }
        
        public RequestStatus Status { get; set; } = RequestStatus.Open;
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? RequiredDate { get; set; }
        
        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;
        
        [ForeignKey("SiteId")]
        public Site Site { get; set; } = null!;
        
        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    }
}
