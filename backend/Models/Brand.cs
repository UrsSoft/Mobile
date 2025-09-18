namespace SantiyeTalepApi.Models
{
    public class Brand
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Many-to-many relationship with Category through CategoryBrand
        public ICollection<CategoryBrand> CategoryBrands { get; set; } = new List<CategoryBrand>();
        
        // Many-to-many relationship with Site through SiteBrand
        public ICollection<SiteBrand> SiteBrands { get; set; } = new List<SiteBrand>();
    }
}
