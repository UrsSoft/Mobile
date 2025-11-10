using Microsoft.AspNetCore.Mvc;
using SantiyeTalepWebUI.Models;
using SantiyeTalepWebUI.Models.DTOs;
using SantiyeTalepWebUI.Models.ViewModels;
using SantiyeTalepWebUI.Services;
using SantiyeTalepWebUI.Filters;

namespace SantiyeTalepWebUI.Controllers
{
    [AuthorizeRole(UserRole.Employee)]
    public class EmployeeController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IApiService apiService, IAuthService authService, ILogger<EmployeeController> logger)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Dashboard - No token found, redirecting to login");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _logger.LogInformation("=== EMPLOYEE DASHBOARD LOADING STARTED ===");
                _logger.LogInformation("Token present: {TokenLength} characters", token.Length);

                // Use employee-specific endpoint that returns EmployeeRequestDto without offers
                _logger.LogInformation("Fetching employee requests from api/Request/employee");
                var myRequests = await _apiService.GetAsync<List<EmployeeRequestDto>>("api/Request/employee", token) ?? new List<EmployeeRequestDto>();
                _logger.LogInformation("Loaded {Count} requests for employee", myRequests.Count);

                SiteDto? mySite = null;
                try
                {
                    _logger.LogInformation("=== FETCHING SITE INFORMATION ===");
                    _logger.LogInformation("API BaseUrl: {BaseUrl}", _apiService.BaseUrl);
                    _logger.LogInformation("Calling endpoint: api/Request/my-site");

                    mySite = await _apiService.GetAsync<SiteDto>("api/Request/my-site", token);

                    if (mySite != null)
                    {
                        _logger.LogInformation("✅ Site info loaded successfully:");
                        _logger.LogInformation("  - Site Name: {SiteName}", mySite.Name);
                        _logger.LogInformation("  - Site ID: {SiteId}", mySite.Id);
                        _logger.LogInformation("  - Brand Count: {BrandCount}", mySite.Brands?.Count ?? 0);

                        if (mySite.Brands != null && mySite.Brands.Any())
                        {
                            _logger.LogInformation("  - Brands: {BrandNames}", string.Join(", ", mySite.Brands.Select(b => b.Name)));
                        }
                        else
                        {
                            _logger.LogWarning("  - ⚠️ Site has NO brands assigned!");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("❌ Site info returned NULL from API");
                        _logger.LogWarning("This means the employee may not be assigned to a site");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError("❌ HTTP ERROR while fetching site info:");
                    _logger.LogError("  - Status Code: {StatusCode}", httpEx.StatusCode);
                    _logger.LogError("  - Message: {Message}", httpEx.Message);
                    _logger.LogError("  - Stack Trace: {StackTrace}", httpEx.StackTrace);

                    // 400 hatası - muhtemelen şantiye ataması yok veya şantiyede marka yok
                    if (httpEx.Message.Contains("400"))
                    {
                        _logger.LogWarning("Bad Request (400) - Employee has no assigned site or site has no brands");

                        // Kullanıcıya bilgi mesajı göster
                        if (httpEx.Message.Contains("atanmadınız"))
                        {
                            TempData["InfoMessage"] = "Henüz bir şantiyeye atanmadınız. Lütfen yöneticinizle iletişime geçin.";
                            _logger.LogInformation("Set InfoMessage: Employee not assigned to site");
                        }
                        else if (httpEx.Message.Contains("marka bulunmuyor"))
                        {
                            TempData["InfoMessage"] = "Şantiyenizde kayıtlı marka bulunmuyor. Lütfen yöneticinizle iletişime geçin.";
                            _logger.LogInformation("Set InfoMessage: Site has no brands");
                        }
                        else
                        {
                            TempData["WarningMessage"] = "Şantiye bilgisi alınamadı: " + httpEx.Message;
                        }
                    }
                    else if (httpEx.Message.Contains("401"))
                    {
                        _logger.LogError("Unauthorized (401) - Token may be invalid or expired");
                        TempData["ErrorMessage"] = "Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Şantiye bilgisi yüklenemedi. Dashboard kısıtlı özelliklerle açılıyor.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ UNEXPECTED ERROR while loading site info:");
                    _logger.LogError("  - Exception Type: {ExceptionType}", ex.GetType().Name);
                    _logger.LogError("  - Message: {Message}", ex.Message);
                    _logger.LogError("  - Stack Trace: {StackTrace}", ex.StackTrace);

                    TempData["WarningMessage"] = "Şantiye bilgisi yüklenirken beklenmeyen bir hata oluştu.";
                    // Site bilgisi yüklenemese bile dashboard'u göster
                }

                var stats = new DashboardStats
                {
                    MyRequests = myRequests.Count,
                    OpenRequests = myRequests.Count(r => r.Status == RequestStatus.Open),
                    CompletedRequests = myRequests.Count(r => r.Status == RequestStatus.Completed)
                };

                var model = new EmployeeDashboardViewModel
                {
                    Stats = stats,
                    MyRequests = myRequests.Take(10).ToList(),
                    MySite = mySite  // null olabilir
                };

                _logger.LogInformation("=== EMPLOYEE DASHBOARD LOADED SUCCESSFULLY ===");
                _logger.LogInformation("  - Total Requests: {TotalRequests}", stats.MyRequests);
                _logger.LogInformation("  - Open Requests: {OpenRequests}", stats.OpenRequests);
                _logger.LogInformation("  - Completed Requests: {CompletedRequests}", stats.CompletedRequests);
                _logger.LogInformation("  - Site Info: {SiteStatus}", mySite != null ? "Available" : "NOT Available");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CRITICAL ERROR loading employee dashboard:");
                _logger.LogError("  - Exception Type: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("  - Message: {Message}", ex.Message);
                _logger.LogError("  - Stack Trace: {StackTrace}", ex.StackTrace);

                TempData["ErrorMessage"] = "Dashboard yüklenirken kritik bir hata oluştu. Lütfen tekrar deneyin.";
                return View(new EmployeeDashboardViewModel());
            }
        }

        public async Task<IActionResult> Requests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var requests = await _apiService.GetAsync<List<EmployeeRequestDto>>("api/Request/employee", token) ?? new List<EmployeeRequestDto>();
                var model = new SantiyeTalepWebUI.Models.DTOs.EmployeeRequestListViewModel
                {
                    Requests = requests,
                    TotalRequests = requests.Count,
                    OpenRequests = requests.Count(r => r.Status == RequestStatus.Open),
                    CompletedRequests = requests.Count(r => r.Status == RequestStatus.Completed)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee requests");
                var model = new SantiyeTalepWebUI.Models.DTOs.EmployeeRequestListViewModel();
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult CreateRequest()
        {
            return View(new CreateRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(CreateRequestViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var createDto = new CreateRequestDto
                {
                    ProductDescription = model.ProductDescription,
                    Quantity = model.Quantity,
                    DeliveryType = model.DeliveryType,
                    Description = model.Description
                };

                var result = await _apiService.PostAsync<object>("api/Request", createDto, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Talep başarıyla oluşturuldu";
                    return RedirectToAction("Dashboard");
                }

                TempData["ErrorMessage"] = "Talep oluşturulurken bir hata oluştu";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request");
                TempData["ErrorMessage"] = "Talep oluşturulurken bir hata oluştu";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMySiteInfo()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("GetMySiteInfo - No token found");
                return Json(new { success = false, message = "Oturum süresi dolmuş" });
            }

            try
            {
                _logger.LogInformation("GetMySiteInfo - Starting to fetch site info from backend API");
                _logger.LogInformation("API BaseUrl: {BaseUrl}", _apiService.BaseUrl);

                // Backend API endpoint: /api/Request/my-site
                var mySite = await _apiService.GetAsync<SiteDto>("api/Request/my-site", token);

                _logger.LogInformation("GetMySiteInfo - Site info response received from API");

                if (mySite == null)
                {
                    _logger.LogWarning("GetMySiteInfo - Site info is null from backend");
                    return Json(new
                    {
                        success = false,
                        message = "Henüz bir şantiyeye atanmadınız. Lütfen yöneticinizle iletişime geçin.",
                        hasNoSite = true
                    });
                }

                // ✅ Şantiyenin markaları var mı kontrol et
                if (mySite.Brands == null || !mySite.Brands.Any())
                {
                    _logger.LogWarning("GetMySiteInfo - Site {SiteName} (ID: {SiteId}) has no brands",
                        mySite.Name, mySite.Id);
                    return Json(new
                    {
                        success = false,
                        message = $"Şantiyeniz ({mySite.Name}) kayıtlı marka bulunmuyor. Lütfen yöneticinizle iletişime geçin.",
                        siteName = mySite.Name,
                        siteId = mySite.Id,
                        hasNoBrands = true
                    });
                }

                var brands = mySite.Brands.Select(b => new {
                    id = b.Id,
                    name = b.Name
                }).Cast<object>().ToList();

                _logger.LogInformation("GetMySiteInfo - Site {SiteName} (ID: {SiteId}) has {BrandCount} brands: {BrandNames}",
                    mySite.Name, mySite.Id, brands.Count, string.Join(", ", mySite.Brands.Select(b => b.Name)));

                _logger.LogInformation("GetMySiteInfo - Returning success response");
                return Json(new
                {
                    success = true,
                    site = new
                    {
                        id = mySite.Id,
                        name = mySite.Name,
                        brands = brands
                    }
                });
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("400"))
            {
                // ✅ Backend'den 400 hatası gelirse (hasNoSite veya hasNoBrands durumu)
                _logger.LogWarning(httpEx, "GetMySiteInfo - Bad Request from backend API (likely no site or no brands)");

                // Hata mesajını parse et
                string errorMessage = "Şantiye bilgisi alınamadı";
                if (httpEx.Message.Contains("Henüz bir şantiyeye atanmadınız"))
                {
                    errorMessage = "Henüz bir şantiyeye atanmadınız. Lütfen yöneticinizle iletişime geçin.";
                }
                else if (httpEx.Message.Contains("kayıtlı marka bulunmuyor"))
                {
                    errorMessage = "Şantiyenizde kayıtlı marka bulunmuyor. Lütfen yöneticinizle iletişime geçin.";
                }

                return Json(new
                {
                    success = false,
                    message = errorMessage,
                    hasNoSite = httpEx.Message.Contains("atanmadınız"),
                    hasNoBrands = httpEx.Message.Contains("marka bulunmuyor")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMySiteInfo - Error getting site info from backend API");
                return Json(new
                {
                    success = false,
                    message = "Şantiye bilgisi alınırken hata oluştu. Lütfen yöneticinizle iletişime geçin.",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string query)
        {
            _logger.LogInformation("=== EMPLOYEE SEARCH PRODUCTS STARTED ===");
            _logger.LogInformation("Query: {Query}", query);

            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found for SearchProducts");
                return Json(new { success = false, message = "Oturum süresi dolmuş" });
            }

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { success = false, message = "Arama terimi en az 2 karakter olmalıdır" });
            }

            try
            {
                // ✅ Employee için direkt teklifalani API'sine istek at
                // Backend otomatik olarak şantiye markalarına göre filtreler
                var searchUrl = $"https://teklifalani.com/api/searchapi?query={Uri.EscapeDataString(query)}";

                _logger.LogInformation("Calling search API: {Url}", searchUrl);

                // ✅ API'nin wrapper yapısını kullan
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var response = await httpClient.GetAsync(searchUrl);
                var jsonContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Content: {Content}", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API returned error: {StatusCode}", response.StatusCode);
                    return Json(new { success = false, message = "Ürün arama API'si hata döndü" });
                }

                // ✅ Response wrapper'ını parse et
                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<Models.ViewModels.ProductSearchApiResponse>(
                    jsonContent,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                if (apiResponse == null || !apiResponse.Success)
                {
                    _logger.LogWarning("API returned unsuccessful response or null");
                    return Json(new { success = false, message = apiResponse?.Message ?? "Ürün bulunamadı" });
                }

                // ✅ API modelini frontend modeline dönüştür
                var products = apiResponse.Data
                    .Select(Models.ViewModels.ProductDto.FromApiDto)
                    .ToList();

                _logger.LogInformation("=== SEARCH COMPLETED ===");
                _logger.LogInformation("Found {Count} products", products.Count);

                return Json(new
                {
                    success = true,
                    data = products,
                    count = products.Count,
                    message = $"{products.Count} ürün bulundu"
                });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error in SearchProducts");
                return Json(new { success = false, message = "Ürün verileri işlenirken hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products for query: '{Query}'", query);
                return Json(new
                {
                    success = false,
                    message = "Ürün arama sırasında hata oluştu",
                    error = ex.Message
                });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SearchProducts([FromBody] ProductSearchDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var products = await _apiService.PostAsync<List<ProductDto>>("api/Request/search-products", model, token) ?? new List<ProductDto>();
                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return Json(new List<ProductDto>());
            }
        }

        public async Task<IActionResult> RequestDetails(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                // Use employee-specific endpoint that returns EmployeeRequestDto without offers
                var request = await _apiService.GetAsync<EmployeeRequestDto>($"api/Request/employee/{id}", token);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Talep bulunamadı";
                    return RedirectToAction("Requests");
                }

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request details");
                TempData["ErrorMessage"] = "Talep detayları yüklenirken bir hata oluştu";
                return RedirectToAction("Requests");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var result = await _apiService.PutAsync<object>($"api/Request/{id}/cancel", new { }, token);
                if (result != null)
                {
                    return Json(new { success = true, message = "Talep başarıyla iptal edildi" });
                }

                return Json(new { success = false, message = "Talep iptal edilirken bir hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling request");
                return Json(new { success = false, message = "Talep iptal edilirken bir hata oluştu" });
            }
        }

        // Notification Management Methods
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var notifications = await _apiService.GetAsync<List<Models.DTOs.NotificationDto>>("api/Notification", token) ?? new List<Models.DTOs.NotificationDto>();
                return Json(new { success = true, data = notifications });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return Json(new { success = false, message = "Bildirimler yüklenirken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationSummary()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var summary = await _apiService.GetAsync<Models.DTOs.NotificationSummaryDto>("api/Notification/summary", token);
                return Json(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification summary");
                return Json(new { success = false, message = "Bildirim özeti yüklenirken hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead([FromBody] int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                _logger.LogInformation("MarkNotificationAsRead called with ID: {NotificationId}", id);

                await _apiService.PutAsync<dynamic>($"api/Notification/{id}/read", new { }, token);

                _logger.LogInformation("Backend API response received for notification {NotificationId}", id);

                return Json(new { success = true, message = "Bildirim okundu olarak işaretlendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return Json(new { success = false, message = "Bildirim güncellenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                await _apiService.PutAsync<dynamic>("api/Notification/mark-all-read", new { }, token);
                return Json(new { success = true, message = "Tüm bildirimler okundu olarak işaretlendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "Bildirimler güncellenirken hata oluştu" });
            }
        }
    }
}