using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SantiyeTalepApi.Models
{
    public class Employee
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int SiteId { get; set; }
        
        public string Position { get; set; } = string.Empty;
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        [ForeignKey("SiteId")]
        public Site Site { get; set; } = null!;
        
        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}
