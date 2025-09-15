using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.Models
{
    public class Site
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation Properties
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}
