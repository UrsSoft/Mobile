using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.DTOs
{
    public class SiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateSiteDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
