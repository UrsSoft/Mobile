namespace SantiyeTalepApi.Models
{
    public class SiteBrand
    {
        public int SiteId { get; set; }
        public Site Site { get; set; } = null!;

        public int BrandId { get; set; }
        public Brand Brand { get; set; } = null!;
    }
}