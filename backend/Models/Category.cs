namespace SantiyeTalepApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Many-to-many relationship with Brand through CategoryBrand
        public List<CategoryBrand> CategoryBrands { get; set; }
    }
}
