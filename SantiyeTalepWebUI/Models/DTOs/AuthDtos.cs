using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

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

    public class SupplierRegisterDto
    {
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şirket adı gereklidir")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi Numarası gereklidir")]
        public string TaxNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres bilgisi gereklidir")]
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string Phone { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mevcut şifre gereklidir")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Token gereklidir")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}