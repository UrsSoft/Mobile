using System.ComponentModel.DataAnnotations;
using SantiyeTalepApi.Models;

namespace SantiyeTalepApi.DTOs
{
    public class SupplierDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public SupplierStatus Status { get; set; }
        public string? ApprovalNote { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon gereklidir")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şirket adı gereklidir")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası gereklidir")]
        public string TaxNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres gereklidir")]
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateSupplierDto
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon gereklidir")]
        public string Phone { get; set; } = string.Empty;

        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Şirket adı gereklidir")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası gereklidir")]
        public string TaxNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres gereklidir")]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class SupplierStatusUpdateDto
    {
        public SupplierStatus Status { get; set; }
        public string? Note { get; set; }
        public string? RejectionReason { get; set; }
    }
}