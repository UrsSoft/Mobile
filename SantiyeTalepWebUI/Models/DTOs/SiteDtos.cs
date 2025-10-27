using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class SiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<EmployeeDto>? Employees { get; set; }
        public List<BrandDto> Brands { get; set; } = new List<BrandDto>();
    }

    public class CreateSiteDto
    {
        [Required(ErrorMessage = "Þantiye adý gereklidir")]
        [MinLength(2, ErrorMessage = "Þantiye adý en az 2 karakter olmalýdýr")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres gereklidir")]
        public string Address { get; set; } = string.Empty;
        
        public string? Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "En az bir marka seçilmelidir")]
        public List<int> BrandIds { get; set; } = new List<int>();
    }

    public class UpdateSiteDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Þantiye adý gereklidir")]
        [MinLength(2, ErrorMessage = "Þantiye adý en az 2 karakter olmalýdýr")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres gereklidir")]
        public string Address { get; set; } = string.Empty;
        
        public string? Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "En az bir marka seçilmelidir")]
        public List<int> BrandIds { get; set; } = new List<int>();
        
        // Çalýþan seçimi (zorunlu deðil)
        public List<int> EmployeeIds { get; set; } = new List<int>();
        
        public bool IsActive { get; set; } = true;
    }

    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}