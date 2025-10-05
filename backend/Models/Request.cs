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
        public string ProductDescription { get; set; } = string.Empty; // Ürün açıklaması
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } // Miktar
        
        [Required]
        public DeliveryType DeliveryType { get; set; } // Teslim Tipi
        
        public string? Description { get; set; } = string.Empty; // Açıklama
        
        public RequestStatus Status { get; set; } = RequestStatus.Open;
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;
        
        [ForeignKey("SiteId")]
        public Site Site { get; set; } = null!;
        
        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    }
}
