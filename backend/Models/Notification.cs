using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        // Admin bildirimler için UserId null olabilir
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        // Ýlgili talep ID'si (varsa)
        public int? RequestId { get; set; }
        public Request? Request { get; set; }
        
        // Ýlgili teklif ID'si (varsa)  
        public int? OfferId { get; set; }
        public Offer? Offer { get; set; }
        
        // Ýlgili tedarikçi ID'si (varsa)
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
    }
}