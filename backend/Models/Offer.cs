using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SantiyeTalepApi.Models
{
    public class Offer
    {
        public int Id { get; set; }
        
        [Required]
        public int RequestId { get; set; }
        
        [Required]
        public int SupplierId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public int DeliveryDays { get; set; }
        
        public OfferStatus Status { get; set; } = OfferStatus.Pending;
        
        public DateTime OfferDate { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("RequestId")]
        public Request Request { get; set; } = null!;
        
        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; } = null!;
    }
}
