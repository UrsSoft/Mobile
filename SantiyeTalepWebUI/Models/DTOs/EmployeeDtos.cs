using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þifre gereklidir")]
        [MinLength(6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarasý gereklidir")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pozisyon gereklidir")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þantiye seçimi gereklidir")]
        public int SiteId { get; set; }
    }
}