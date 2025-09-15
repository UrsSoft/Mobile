using SantiyeTalepWebUI.Models.DTOs;

namespace SantiyeTalepWebUI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<bool> RegisterSupplierAsync(SupplierRegisterDto registerDto);
        Task<bool> LogoutAsync();
        Task<UserDto?> UpdateProfileAsync(UpdateProfileDto updateDto, string token);
        Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto, string token);
        string? GetStoredToken();
        void StoreToken(string token);
        void ClearToken();
        UserDto? GetCurrentUser();
        void SetCurrentUser(UserDto user);
        void ClearCurrentUser();
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

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var response = await _apiService.PostAsync<LoginResponseDto>("api/Auth/login", loginDto);
                
                if (response != null)
                {
                    StoreToken(response.Token);
                    SetCurrentUser(response.User);
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
                        SetCurrentUser(userDto);
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
            return _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
        }

        public void StoreToken(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("AuthToken", token);
        }

        public void ClearToken()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("AuthToken");
        }

        public UserDto? GetCurrentUser()
        {
            var userJson = _httpContextAccessor.HttpContext?.Session.GetString("CurrentUser");
            if (!string.IsNullOrEmpty(userJson))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<UserDto>(userJson);
            }
            return null;
        }

        public void SetCurrentUser(UserDto user)
        {
            var userJson = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            _httpContextAccessor.HttpContext?.Session.SetString("CurrentUser", userJson);
        }

        public void ClearCurrentUser()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("CurrentUser");
        }
    }
}