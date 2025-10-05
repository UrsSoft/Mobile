using SantiyeTalepWebUI.Models.DTOs;

namespace SantiyeTalepWebUI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto, bool rememberMe = false);
        Task<bool> RegisterSupplierAsync(SupplierRegisterDto registerDto);
        Task<bool> LogoutAsync();
        Task<UserDto?> UpdateProfileAsync(UpdateProfileDto updateDto, string token);
        Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto, string token);
        string? GetStoredToken();
        void StoreToken(string token, bool rememberMe = false);
        void ClearToken();
        UserDto? GetCurrentUser();
        void SetCurrentUser(UserDto user, bool rememberMe = false);
        void ClearCurrentUser();
        bool IsAuthenticated();
        void InitializeFromCookies();
    }

    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IApiService apiService, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto, bool rememberMe = false)
        {
            try
            {
                var response = await _apiService.PostAsync<LoginResponseDto>("api/Auth/login", loginDto);
                
                if (response != null)
                {
                    StoreToken(response.Token, rememberMe);
                    SetCurrentUser(response.User, rememberMe);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return null;
            }
        }

        public async Task<bool> RegisterSupplierAsync(SupplierRegisterDto registerDto)
        {
            try
            {
                var response = await _apiService.PostAsync<object>("api/Auth/register-supplier", registerDto);
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during supplier registration");
                return false;
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var token = GetStoredToken();
                if (!string.IsNullOrEmpty(token))
                {
                    await _apiService.PostAsync<object>("api/Auth/logout", new { }, token);
                }

                ClearToken();
                ClearCurrentUser();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<UserDto?> UpdateProfileAsync(UpdateProfileDto updateDto, string token)
        {
            try
            {
                var response = await _apiService.PutAsync<dynamic>("api/Auth/update-profile", updateDto, token);
                
                if (response?.user != null)
                {
                    var userDto = Newtonsoft.Json.JsonConvert.DeserializeObject<UserDto>(response.user.ToString());
                    if (userDto != null)
                    {
                        // Check if user was remembered and maintain that state
                        bool isRemembered = !string.IsNullOrEmpty(_httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"]);
                        SetCurrentUser(userDto, isRemembered);
                    }
                    return userDto;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return null;
            }
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto, string token)
        {
            try
            {
                var response = await _apiService.PutAsync<object>("api/Auth/change-password", changePasswordDto, token);
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return false;
            }
        }

        public string? GetStoredToken()
        {
            // First try to get from session
            var sessionToken = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            if (!string.IsNullOrEmpty(sessionToken))
            {
                return sessionToken;
            }

            // If not in session, try to get from cookie
            var cookieToken = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(cookieToken))
            {
                // Store in session for current request
                _httpContextAccessor.HttpContext?.Session.SetString("AuthToken", cookieToken);
                return cookieToken;
            }

            return null;
        }

        public void StoreToken(string token, bool rememberMe = false)
        {
            // Always store in session
            _httpContextAccessor.HttpContext?.Session.SetString("AuthToken", token);

            // If remember me is checked, also store in cookie
            if (rememberMe && _httpContextAccessor.HttpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30), // Remember for 30 days
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };

                _httpContextAccessor.HttpContext.Response.Cookies.Append("AuthToken", token, cookieOptions);
            }
        }

        public void ClearToken()
        {
            // Clear from session
            _httpContextAccessor.HttpContext?.Session.Remove("AuthToken");
            
            // Clear from cookies
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Delete("AuthToken");
            }
        }

        public UserDto? GetCurrentUser()
        {
            // First try to get from session
            var userJson = _httpContextAccessor.HttpContext?.Session.GetString("CurrentUser");
            if (!string.IsNullOrEmpty(userJson))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<UserDto>(userJson);
            }

            // If not in session, try to get from cookie
            var cookieUserJson = _httpContextAccessor.HttpContext?.Request.Cookies["CurrentUser"];
            if (!string.IsNullOrEmpty(cookieUserJson))
            {
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<UserDto>(cookieUserJson);
                if (user != null)
                {
                    // Store in session for current request
                    _httpContextAccessor.HttpContext?.Session.SetString("CurrentUser", cookieUserJson);
                    return user;
                }
            }

            return null;
        }

        public void SetCurrentUser(UserDto user, bool rememberMe = false)
        {
            var userJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            
            // Always store in session
            _httpContextAccessor.HttpContext?.Session.SetString("CurrentUser", userJson);

            // If remember me is checked, also store in cookie
            if (rememberMe && _httpContextAccessor.HttpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30), // Remember for 30 days
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };

                _httpContextAccessor.HttpContext.Response.Cookies.Append("CurrentUser", userJson, cookieOptions);
            }
        }

        public void ClearCurrentUser()
        {
            // Clear from session
            _httpContextAccessor.HttpContext?.Session.Remove("CurrentUser");
            
            // Clear from cookies
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Delete("CurrentUser");
            }
        }

        public bool IsAuthenticated()
        {
            var token = GetStoredToken();
            var user = GetCurrentUser();
            return !string.IsNullOrEmpty(token) && user != null;
        }

        public void InitializeFromCookies()
        {
            // This method can be called on application start to restore session from cookies
            var token = GetStoredToken(); // This will automatically move cookie to session if exists
            var user = GetCurrentUser(); // This will automatically move cookie to session if exists
        }
    }
}