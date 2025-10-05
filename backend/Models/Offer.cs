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
        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Required]
        public Currency Currency { get; set; } = Currency.TRY;
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal Discount { get; set; } = 0;
        
        [Required]
        public DeliveryType DeliveryType { get; set; }
        
        public int DeliveryDays { get; set; }
        
        public OfferStatus Status { get; set; } = OfferStatus.Pending;
        
        public DateTime OfferDate { get; set; } = DateTime.UtcNow;
        
        // Calculated properties
        [NotMapped]
        public decimal TotalPrice => Price * Quantity;
        
        [NotMapped]
        public decimal DiscountAmount => (TotalPrice * Discount) / 100;
        
        [NotMapped]
        public decimal FinalPrice => TotalPrice - DiscountAmount;
        
        // Navigation Properties
        [ForeignKey("RequestId")]
        public Request Request { get; set; } = null!;
        
        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; } = null!;
    }
}
