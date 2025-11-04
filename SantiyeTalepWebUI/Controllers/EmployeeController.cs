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
                return RedirectToAction("Login", "Account");

            try
            {
                // Use employee-specific endpoint that returns EmployeeRequestDto without offers
                var myRequests = await _apiService.GetAsync<List<EmployeeRequestDto>>("api/Request/employee", token) ?? new List<EmployeeRequestDto>();
                var mySite = await _apiService.GetAsync<SiteDto>("api/Request/my-site", token);

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
                    MySite = mySite
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee dashboard");
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
                var requests = await _apiService.GetAsync<List<EmployeeRequestDto>>("api/Employee/my-requests", token) ?? new List<EmployeeRequestDto>();
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
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var mySite = await _apiService.GetAsync<SiteDto>("api/Request/my-site", token);
                if (mySite == null)
                {
                    return Json(new { success = false, message = "Şantiye bilgisi bulunamadı" });
                }

                var brands = new List<object>();
                if (mySite.Brands != null)
                {
                    brands = mySite.Brands.Select(b => new {
                        id = b.Id,
                        name = b.Name
                    }).Cast<object>().ToList();
                }

                return Json(new { 
                    success = true, 
                    site = new {
                        id = mySite.Id,
                        name = mySite.Name,
                        brands = brands
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site info");
                return Json(new { success = false, message = "Şantiye bilgisi alınırken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string query, string brandIds = "")
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { success = false, message = "Arama terimi en az 2 karakter olmalıdır" });
            }

            try
            {
                // Build search URL with brand filtering
                var searchUrl = $"api/searchapi?query={Uri.EscapeDataString(query)}";
                
                // Add brand IDs if provided
                if (!string.IsNullOrEmpty(brandIds))
                {
                    searchUrl += $"&brandIds={brandIds}";
                }

                var products = await _apiService.GetAsync<List<ProductDto>>(searchUrl, token) ?? new List<ProductDto>();
                
                return Json(new { 
                    success = true, 
                    data = products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return Json(new { success = false, message = "Ürün arama sırasında hata oluştu" });
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