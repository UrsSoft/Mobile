using System.ComponentModel.DataAnnotations;

namespace SantiyeTalepApi.DTOs
{
    // Flattened EmployeeDto for frontend compatibility
    public class EmployeeDto
    {
        public int Id { get; set; }
        
        // Flattened User properties
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Employee specific properties
        public string Position { get; set; } = string.Empty;
        public int SiteId { get; set; }
        
        // Flattened Site properties
        public string SiteName { get; set; } = string.Empty;
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

    public class UpdateEmployeeDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarasý gereklidir")]
        public string Phone { get; set; } = string.Empty;

        [MinLength(6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Pozisyon gereklidir")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Þantiye seçimi gereklidir")]
        public int SiteId { get; set; }
        
        public bool IsActive { get; set; }
    }
}