using SantiyeTalepApi.Models;
using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public UserRole Role { get; set; }
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
        [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pozisyon gereklidir")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þantiye seçimi gereklidir")]
        public int SiteId { get; set; }
    }

    public class UpdateEmployeeDto
    {
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string? Email { get; set; }

        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz")]
        public string? Phone { get; set; }

        public string? Position { get; set; }

        public int SiteId { get; set; }
    }

    // Flattened EmployeeDto for frontend compatibility
    public class EmployeeDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SiteId { get; set; }
        public string Position { get; set; } = string.Empty;
        
        // Flattened User properties
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Flattened Site properties
        public string SiteName { get; set; } = string.Empty;
    }

    // Flattened SupplierDto for frontend compatibility
    public class SupplierDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public SupplierStatus Status { get; set; }
        
        // Flattened User properties
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
