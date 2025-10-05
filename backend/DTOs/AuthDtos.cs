using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Telefon numarasý gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þifre gereklidir")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    public class SupplierRegisterDto
    {
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þifre gereklidir")]
        [MinLength(6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þirket adý gereklidir")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarasý gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz")]
        public string Phone { get; set; } = string.Empty;
        
        public string TaxNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Geçerli bir telefon numarasý giriniz")]
        public string Phone { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mevcut þifre gereklidir")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni þifre gereklidir")]
        [MinLength(6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þifre tekrarý gereklidir")]
        [Compare("NewPassword", ErrorMessage = "Þifreler eþleþmiyor")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
