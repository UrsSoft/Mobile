using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.DTOs
{
    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateBrandDto
    {
        [Required(ErrorMessage = "Marka adý gereklidir")]
        [MinLength(2, ErrorMessage = "Marka adý en az 2 karakter olmalýdýr")]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateBrandDto
    {
        [Required(ErrorMessage = "Marka adý gereklidir")]
        [MinLength(2, ErrorMessage = "Marka adý en az 2 karakter olmalýdýr")]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}