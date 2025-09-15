using SantiyeTalepWebUI.Models.DTOs;

namespace SantiyeTalepWebUI.Models.ViewModels
{
    public class LoginViewModel
    {
        public LoginDto LoginDto { get; set; } = new();
        public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RegisterSupplierViewModel
    {
        public SupplierRegisterDto RegisterDto { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }

    public class ProfileViewModel
    {
        public UserDto User { get; set; } = null!;
        public UpdateProfileDto UpdateProfile { get; set; } = new();
        public ChangePasswordDto ChangePassword { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}