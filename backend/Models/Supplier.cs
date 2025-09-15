using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SantiyeTalepApi.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string CompanyName { get; set; } = string.Empty;
        
        public string TaxNumber { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
        
        public SupplierStatus Status { get; set; } = SupplierStatus.Pending;
        
        public string? ApprovalNote { get; set; }
        
        public string? RejectionReason { get; set; }
        
        public DateTime? ApprovedDate { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    }
}
