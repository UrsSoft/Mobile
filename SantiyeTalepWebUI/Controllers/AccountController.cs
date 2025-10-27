using Microsoft.AspNetCore.Mvc;
using SantiyeTalepWebUI.Models.DTOs;
using SantiyeTalepWebUI.Models.ViewModels;
using SantiyeTalepWebUI.Services;
using Microsoft.AspNetCore.Authentication;

namespace SantiyeTalepWebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IApiService _apiService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, IApiService apiService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Initialize from cookies if user was remembered
            _authService.InitializeFromCookies();
            
            var currentUser = _authService.GetCurrentUser();
            if (currentUser != null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _authService.LoginAsync(model.LoginDto, model.RememberMe);
                
                if (result != null)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    
                    return RedirectToAction("Index", "Home");
                }

                model.ErrorMessage = "Geçersiz telefon numarası veya şifre";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                model.ErrorMessage = "Giriş sırasında bir hata oluştu";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult RegisterSupplier()
        {
            return View(new RegisterSupplierViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterSupplier(RegisterSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Clean phone number - remove spaces and format to clean number for database storage
                if (!string.IsNullOrEmpty(model.RegisterDto.Phone))
                {
                    model.RegisterDto.Phone = model.RegisterDto.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }
                
                var result = await _authService.RegisterSupplierAsync(model.RegisterDto);
                
                if (result)
                {
                    model.SuccessMessage = "Tedarikçi kaydınız başarıyla alındı. Onay bekliyor.";
                    model.RegisterDto = new SupplierRegisterDto(); // Form'u temizle
                    return View(model);
                }

                model.ErrorMessage = "Kayıt sırasında bir hata oluştu";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supplier registration error");
                model.ErrorMessage = "Kayıt sırasında bir hata oluştu";
                return View(model);
            }
        }

        [HttpGet]
        [HttpPost] // Allow both GET and POST for logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _authService.LogoutAsync();
                
                // Clear any additional authentication cookies if they exist
                try
                {
                    if (HttpContext.User.Identity?.IsAuthenticated == true)
                    {
                        await HttpContext.SignOutAsync();
                    }
                }
                catch
                {
                    // Ignore authentication errors during logout
                }
                
                // Clear session completely
                HttpContext.Session.Clear();
                
                TempData["Message"] = "Başarıyla çıkış yaptınız.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                // Even if there's an error, redirect to login
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult Profile()
        {
            // Initialize from cookies if user was remembered
            _authService.InitializeFromCookies();
            
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                User = currentUser,
                UpdateProfile = new UpdateProfileDto
                {
                    FullName = currentUser.FullName,
                    Phone = currentUser.Phone
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                model.User = currentUser;
                return View("Profile", model);
            }

            try
            {
                var result = await _authService.UpdateProfileAsync(model.UpdateProfile, token);
                
                if (result != null)
                {
                    model.SuccessMessage = "Profil başarıyla güncellendi";
                    model.User = result;
                }
                else
                {
                    model.ErrorMessage = "Profil güncellenirken bir hata oluştu";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile update error");
                model.ErrorMessage = "Profil güncellenirken bir hata oluştu";
            }

            return View("Profile", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                model.User = currentUser;
                return View("Profile", model);
            }

            try
            {
                var result = await _authService.ChangePasswordAsync(model.ChangePassword, token);
                
                if (result)
                {
                    model.SuccessMessage = "Şifre başarıyla değiştirildi";
                    model.ChangePassword = new ChangePasswordDto(); // Form'u temizle
                }
                else
                {
                    model.ErrorMessage = "Şifre değiştirilirken bir hata oluştu";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change error");
                model.ErrorMessage = "Şifre değiştirilirken bir hata oluştu";
            }

            model.User = currentUser;
            return View("Profile", model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var forgotPasswordDto = new ForgotPasswordDto
                {
                    Email = model.Email
                };

                var response = await _apiService.PostAsync<dynamic>("api/Auth/forgot-password", forgotPasswordDto);
                
                model.SuccessMessage = "Eğer bu e-posta adresi sistemde kayıtlıysa, şifre sıfırlama bağlantısı gönderilecektir.";
                model.Email = string.Empty; // Form'u temizle
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password error");
                model.ErrorMessage = "Şifre sıfırlama işlemi sırasında bir hata oluştu";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var resetPasswordDto = new ResetPasswordDto
                {
                    Token = model.Token,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword
                };

                var response = await _apiService.PostAsync<dynamic>("api/Auth/reset-password", resetPasswordDto);
                
                if (response != null)
                {
                    model.SuccessMessage = "Şifreniz başarıyla sıfırlandı. Şimdi giriş yapabilirsiniz.";
                    model.NewPassword = string.Empty;
                    model.ConfirmPassword = string.Empty;
                    return View(model);
                }

                model.ErrorMessage = "Şifre sıfırlama işlemi sırasında bir hata oluştu";
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Reset password error");
                
                // Parse error message from exception
                var errorMessage = ex.Message;
                if (errorMessage.StartsWith("400:"))
                {
                    errorMessage = errorMessage.Substring(5).Trim();
                }
                
                model.ErrorMessage = errorMessage;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reset password error");
                model.ErrorMessage = "Şifre sıfırlama işlemi sırasında bir hata oluştu";
                return View(model);
            }
        }
    }
}