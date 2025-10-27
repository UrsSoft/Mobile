using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SantiyeTalepApi.Models
{
    public class Employee
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public int? SiteId { get; set; } // Nullable - çalýþan þantiye atanmamýþ olabilir
        
        public string Position { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        [ForeignKey("SiteId")]
        public Site? Site { get; set; } // Nullable - site atanmamýþ olabilir
        
        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}
