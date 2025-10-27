using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;
        
        public UserRole Role { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // FCM Token for push notifications
        public string? FcmToken { get; set; }
        
        // Password Reset
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        
        // Navigation Properties
        public Employee? Employee { get; set; }
        public Supplier? Supplier { get; set; }
    }
}
