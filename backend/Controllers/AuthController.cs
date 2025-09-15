using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
using SantiyeTalepApi.Services;
using System.Security.Claims;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;

        public AuthController(ApplicationDbContext context, IJwtService jwtService, IMapper mapper)
        {
            _context = context;
            _jwtService = jwtService;
            _mapper = mapper;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "API çalışıyor", timestamp = DateTime.Now });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user == null)
            {
                Console.WriteLine($"Kullanıcı bulunamadı: {request.Email}");
                return BadRequest(new { message = "Kullanıcı bulunamadı" });
            }

            Console.WriteLine($"Giriş denemesi - Email: {request.Email}");
            Console.WriteLine($"Girilen şifre: {request.Password}");
            Console.WriteLine($"Veritabanındaki hash: {user.Password}");

            // Şifre doğrulama - debug için log ekleyelim
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            Console.WriteLine($"BCrypt verify sonucu: {isValidPassword}");
            
            if (!isValidPassword)
            {
                // Test için doğru hash'i oluşturalım
                string correctHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                Console.WriteLine($"admin123 için yeni hash: {correctHash}");
                
                return BadRequest(new { message = "Geçersiz şifre" });
            }

            if (!user.IsActive)
                return Unauthorized(new { message = "Hesap aktif değil" });

            // Tedarikçi ise onay durumunu kontrol et
            if (user.Role == UserRole.Supplier)
            {
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                
                if (supplier?.Status != SupplierStatus.Approved)
                    return Unauthorized(new { message = "Tedarikçi hesabı henüz onaylanmamış" });
            }

            var token = _jwtService.GenerateToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                User = userDto
            });
        }

        [HttpPost("register-supplier")]
        public async Task<IActionResult> RegisterSupplier([FromBody] SupplierRegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Email kontrolü
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest(new { message = "Bu email adresi zaten kullanılıyor" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // User oluştur
                var user = new User
                {
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Role = UserRole.Supplier,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Supplier oluştur
                var supplier = new Supplier
                {
                    UserId = user.Id,
                    CompanyName = model.CompanyName,
                    TaxNumber = model.TaxNumber,
                    Address = model.Address,
                    Status = SupplierStatus.Pending
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { message = "Tedarikçi kaydı başarılı. Onay bekliyor." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Kayıt sırasında bir hata oluştu" });
            }
        }

        [HttpPost("reset-admin")]
        public async Task<IActionResult> ResetAdmin()
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@santiye.com");
                if (user != null)
                {
                    // admin123 için yeni hash oluştur
                    user.Password = BCrypt.Net.BCrypt.HashPassword("admin123");
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Admin şifresi sıfırlandı" });
                }
                return NotFound(new { message = "Admin kullanıcısı bulunamadı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Client-side'da token silinecek
            return Ok(new { message = "Başarıyla çıkış yapıldı" });
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Geçersiz token" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { message = "Kullanıcı bulunamadı" });

                user.FullName = model.FullName;
                user.Phone = model.Phone;

                await _context.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(user);
                return Ok(new { user = userDto, message = "Profil başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Profil güncellenirken bir hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Geçersiz token" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { message = "Kullanıcı bulunamadı" });

                // Mevcut şifre kontrolü
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
                    return BadRequest(new { message = "Mevcut şifre yanlış" });

                // Yeni şifre kaydet
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Şifre başarıyla değiştirildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şifre değiştirilirken bir hata oluştu", error = ex.Message });
            }
        }
    }
}
