using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SantiyeTalepWebUI.Models;
using SantiyeTalepWebUI.Models.DTOs;
using SantiyeTalepWebUI.Models.ViewModels;
using SantiyeTalepWebUI.Services;
using SantiyeTalepWebUI.Filters;
using System.Text;

namespace SantiyeTalepWebUI.Controllers
{
    [AuthorizeRole(UserRole.Admin)]
    public partial class AdminController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminController> _logger;
        private readonly ICompositeViewEngine _viewEngine;

        public AdminController(IApiService apiService, IAuthService authService, ILogger<AdminController> logger, ICompositeViewEngine viewEngine)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> Dashboard()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                // API'den gerçek istatistikleri çek
                var stats = await _apiService.GetAsync<dynamic>("api/Admin/stats", token);
                var allSuppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers", token) ?? new List<SupplierDto>();
                var recentRequests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                var employees = await _apiService.GetAsync<List<EmployeeDto>>("api/Admin/employees", token) ?? new List<EmployeeDto>();

                // Load notification summary and notifications
                var notificationSummary = await _apiService.GetAsync<Models.DTOs.NotificationSummaryDto>("api/Notification/summary", token);
                var notifications = await _apiService.GetAsync<List<Models.DTOs.NotificationDto>>("api/Notification", token) ?? new List<Models.DTOs.NotificationDto>();

                // Filter pending suppliers from all suppliers
                var pendingSuppliers = allSuppliers.Where(s => s.Status == SupplierStatus.Pending).ToList();

                // Eğer stats null ise varsayılan değerler kullan
                var dashboardStats = new DashboardStats();
                if (stats != null)
                {
                    var statsJson = System.Text.Json.JsonSerializer.Serialize(stats);
                    var statsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(statsJson);

                    if (statsDict != null)
                    {
                        dashboardStats.TotalRequests = GetIntValue(statsDict, "totalRequests");
                        dashboardStats.TotalSites = GetIntValue(statsDict, "activeSites");
                        dashboardStats.PendingSuppliers = pendingSuppliers.Count;
                        // Employee count from actual employees list
                        dashboardStats.TotalUsers = employees.Count;
                    }
                }
                else
                {
                    // If stats API fails, use local counts
                    dashboardStats.TotalRequests = recentRequests.Count;
                    dashboardStats.TotalSites = sites.Count;
                    dashboardStats.PendingSuppliers = pendingSuppliers.Count;
                    dashboardStats.TotalUsers = employees.Count;
                }

                var model = new AdminDashboardViewModel
                {
                    Stats = dashboardStats,
                    PendingSuppliers = pendingSuppliers,
                    RecentRequests = recentRequests.Take(10).ToList(),
                    Sites = sites,
                    Employees = employees.Take(10).ToList(),
                    NotificationSummary = notificationSummary,
                    Notifications = notifications.Take(10).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new AdminDashboardViewModel());
            }
        }

        private int GetIntValue(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return element.GetInt32();
                }
                if (int.TryParse(value.ToString(), out var intValue))
                {
                    return intValue;
                }
            }
            return 0;
        }

        // Sites Management
        public async Task<IActionResult> Sites()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
            return View(sites);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSite()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                // Get brands for selection
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;               

                return View(new Models.DTOs.CreateSiteDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for site creation");
                TempData["ErrorMessage"] = "Sayfa yüklenirken bir hata oluştu";
                ViewBag.Brands = new List<BrandDto>();
                return View(new Models.DTOs.CreateSiteDto());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSite(Models.DTOs.CreateSiteDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                // Reload brands for validation error case
                try
                {
                    var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                    ViewBag.Brands = brands;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading data for validation error case");
                    ViewBag.Brands = new List<BrandDto>();
                }
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<object>("api/Admin/sites", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Şantiye başarıyla oluşturuldu";
                    return RedirectToAction("Sites");
                }

                TempData["ErrorMessage"] = "Şantiye oluşturulurken bir hata oluştu";

                // Reload brands for error case
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;
                
                return View(model);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error creating site");
                
                // Parse error message from API
                string errorMessage = "Şantiye oluşturulurken bir hata oluştu";
                
                if (httpEx.Message.Contains("400:"))
                {
                    // Extract the actual error message from "400: message"
                    var parts = httpEx.Message.Split(new[] { "400:" }, 2, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        errorMessage = parts[1].Trim();
                    }
                }
                else if (httpEx.Message.Contains("markalardan bir kısmı bulunamadı") || httpEx.Message.Contains("aktif değil"))
                {
                    errorMessage = "Seçilen markalardan bir kısmı bulunamadı veya aktif değil. Lütfen geçerli markalar seçin.";
                    ModelState.AddModelError("BrandIds", "Geçersiz marka seçimi");
                }
                
                TempData["ErrorMessage"] = errorMessage;

                // Reload brands for error case
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site");
                TempData["ErrorMessage"] = "Şantiye oluşturulurken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";

                // Reload brands for error case
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;
                
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSiteDetails(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var site = await _apiService.GetAsync<Models.DTOs.SiteDto>($"api/Admin/sites/{id}", token);
                if (site == null)
                    return Json(new { success = false, message = "Şantiye bulunamadı" });

                // Build brands HTML
                var brandsHtml = "";
                if (site.Brands?.Any() == true)
                {
                    brandsHtml = string.Join("", site.Brands.Select(b =>
                        $"<span class='badge bg-primary me-1 mb-1'>{b.Name}</span>"));
                }
                else
                {
                    brandsHtml = "<span class='text-muted'>Henüz marka atanmamış</span>";
                }

                // Build HTML response with enhanced styling
                var html = $@"
                    <div class='row'>
                        <div class='col-md-8'>
                            <div class='card h-100'>
                                <div class='card-header'>
                                    <h6 class='card-title mb-0'>
                                        <i class='ri-building-2-line text-primary me-2'></i>
                                        Şantiye Bilgileri
                                    </h6>
                                </div>
                                <div class='card-body'>
                                    <div class='row g-3'>
                                        <div class='col-12'>
                                            <label class='form-label fw-semibold text-muted'>Şantiye Adı</label>
                                            <p class='mb-0'>{site.Name}</p>
                                        </div>
                                        <div class='col-12'>
                                            <label class='form-label fw-semibold text-muted'>Adres</label>
                                            <p class='mb-0'>
                                                <i class='ri-map-pin-line text-primary me-2'></i>
                                                {site.Address}
                                            </p>
                                        </div>
                                        {(string.IsNullOrEmpty(site.Description) ? "" : $@"
                                        <div class='col-12'>
                                            <label class='form-label fw-semibold text-muted'>Açıklama</label>
                                            <p class='mb-0'>{site.Description}</p>
                                        </div>")}
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Durum</label>
                                            <div>
                                                {(site.IsActive ?
                                                    "<span class='badge bg-success'><i class='ri-checkbox-circle-line me-1'></i>Aktif</span>" :
                                                    "<span class='badge bg-danger'><i class='ri-close-circle-line me-1'></i>Pasif</span>")}
                                            </div>
                                        </div>
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Oluşturulma Tarihi</label>
                                            <div>
                                                <i class='ri-calendar-line text-info me-2'></i>
                                                {site.CreatedDate:dd.MM.yyyy}
                                            </div>
                                        </div>
                                        <div class='col-12'>
                                            <label class='form-label fw-semibold text-muted'>Şantiye Markaları</label>
                                            <div>{brandsHtml}</div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class='col-md-4'>
                            <div class='card h-100'>
                                <div class='card-header'>
                                    <h6 class='card-title mb-0'>
                                        <i class='ri-group-line text-success me-2'></i>
                                        Çalışan Özeti
                                    </h6>
                                </div>
                                <div class='card-body'>
                                    <div class='text-center mb-3'>
                                        <div class='avatar-lg mx-auto mb-2'>
                                            <div class='avatar-title bg-primary-subtle text-primary rounded-circle'>
                                                <i class='ri-team-line display-6'></i>
                                            </div>
                                        </div>
                                        <h4 class='mb-1'>{site.Employees?.Count ?? 0}</h4>
                                        <p class='text-muted mb-0'>Toplam Çalışan</p>
                                    </div>
                                    {(site.Employees?.Any() == true ? $@"
                                    <div class='mt-3 text-center'>
                                        <button type='button' class='btn btn-soft-success btn-sm' onclick='viewEmployees({site.Id})'>
                                            <i class='ri-eye-line me-1'></i>
                                            Tüm Çalışanları Görüntüle
                                        </button>
                                    </div>" : "")}
                                </div>
                            </div>
                        </div>
                    </div>";

                return Json(new { success = true, html = html });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site details");
                return Json(new { success = false, message = "Detaylar yüklenirken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSiteEmployees(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var employees = await _apiService.GetAsync<List<EmployeeDto>>($"api/Admin/sites/{id}/employees", token) ?? new List<EmployeeDto>();

                var html = new StringBuilder();

                if (employees.Any())
                {
                    // Add summary cards
                    var activeCount = employees.Count(e => e.IsActive);
                    var inactiveCount = employees.Count - activeCount;

                    html.Append($@"
                        <div class='row g-3 mb-4'>
                            <div class='col-md-4'>
                                <div class='card text-center'>
                                    <div class='card-body'>
                                        <i class='ri-team-line text-primary display-6 mb-2'></i>
                                        <h4 class='mb-1'>{employees.Count}</h4>
                                        <p class='text-muted mb-0'>Toplam Çalışan</p>
                                    </div>
                                </div>
                            </div>
                            <div class='col-md-4'>
                                <div class='card text-center'>
                                    <div class='card-body'>
                                        <i class='ri-user-line text-success display-6 mb-2'></i>
                                        <h4 class='mb-1 text-success'>{activeCount}</h4>
                                        <p class='text-muted mb-0'>Aktif Çalışan</p>
                                    </div>
                                </div>
                            </div>
                            <div class='col-md-4'>
                                <div class='card text-center'>
                                    <div class='card-body'>
                                        <i class='ri-user-unfollow-line text-warning display-6 mb-2'></i>
                                        <h4 class='mb-1 text-warning'>{inactiveCount}</h4>
                                        <p class='text-muted mb-0'>Pasif Çalışan</p>
                                    </div>
                                </div>
                            </div>
                        </div>");
                    html.Append("<div class='table-responsive'>");
                    html.Append("<table class='table table-hover table-striped'>");
                    html.Append(@"
                        <thead class='table-light'>
                            <tr>
                                <th><i class='ri-user-line me-1'></i>Ad Soyad</th>
                                <th><i class='ri-briefcase-line me-1'></i>Pozison</th>
                                <th><i class='ri-mail-line me-1'></i>E-posta</th>
                                <th><i class='ri-phone-line me-1'></i>Telefon</th>
                                <th><i class='ri-calendar-line me-1'></i>Kayıt Tarihi</th>
                                <th><i class='ri-shield-check-line me-1'></i>Durum</th>
                            </tr>
                        </thead>");
                    html.Append("<tbody>");
                    foreach (var emp in employees.OrderBy(e => e.FullName))
                    {
                        var statusBadge = emp.IsActive ?
                            "<span class='badge bg-success'><i class='ri-checkbox-circle-line me-1'></i>Aktif</span>" :
                            "<span class='badge bg-danger'><i class='ri-close-circle-line me-1'></i>Pasif</span>";

                        var rowClass = emp.IsActive ? "" : "table-secondary";

                        html.Append($@"
                            <tr class='{rowClass}'>
                                <td>
                                    <div class='d-flex align-items-center'>
                                        <div class='avatar-xs me-2'>
                                            <div class='avatar-title bg-primary-subtle text-primary rounded-circle'>
                                                {(emp.FullName?.Substring(0, 1).ToUpper() ?? "?")}
                                            </div>
                                        </div>
                                        <div>
                                            <h6 class='mb-0'>{emp.FullName}</h6>
                                        </div>
                                    </div>
                                </td>
                                <td>
                                    <span class='badge bg-info-subtle text-info'>{emp.Position}</span>
                                </td>
                                <td>
                                    <a href='mailto:{emp.Email}' class='text-decoration-none'>
                                        <i class='ri-mail-line me-1'></i>{emp.Email}
                                    </a>
                                </td>
                                <td>
                                    {(string.IsNullOrEmpty(emp.Phone) ?
                                        "<span class='text-muted'>-</span>" :
                                        $"<a href='tel:{emp.Phone}' class='text-decoration-none'><i class='ri-phone-line me-1'></i>{emp.Phone}</a>")}
                                </td>
                                <td>
                                    <small class='text-muted'>{emp.CreatedDate:dd.MM.yyyy}</small>
                                </td>
                                <td>{statusBadge}</td>
                            </tr>");
                    }

                    html.Append("</tbody></table></div>");
                }
                else
                {
                    var createEmployeeUrl = "/Admin/CreateEmployee";
                    html.Append($@"
                        <div class='text-center py-5'>
                            <div class='d-flex flex-column align-items-center'>
                                <i class='ri-user-line display-4 text-muted mb-3'></i>
                                <h5 class='text-muted'>Bu şantiyede henüz çalışan bulunmuyor</h5>
                                <p class='text-muted mb-4'>Şantiyeye ilk çalışanı ekleyerek başlayın.</p>
                                <button type='button' class='btn btn-primary' onclick='window.location.href=""{createEmployeeUrl}""'>
                                    <i class='ri-add-line me-1'></i> İlk Çalışanı Ekle
                                </button>
                            </div>
                        </div>");
                }

                return Json(new { success = true, html = html.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site employees");
                return Json(new { success = false, message = "Çalışanlar yüklenirken hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSite(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var result = await _apiService.DeleteAsync($"api/Admin/sites/{id}", token);
                return Json(new { success = true, message = "Şantiye başarıyla silindi" });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error deleting site");

                // API'den gelen hata mesajını parse et
                if (httpEx.Message.Contains("400") || httpEx.Message.Contains("Bad Request"))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bu şantiyede çalışan bulunduğu için silinemez. Önce çalışanları başka şantiyelere transfer edin veya hesaplarını silin."
                    });
                }

                return Json(new { success = false, message = "Şantiye silinirken hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site");
                return Json(new { success = false, message = "Şantiye silinirken hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkSiteAction(List<int> siteIds, string action)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var payload = new { siteIds = siteIds, action = action };
                var result = await _apiService.PostAsync<object>("api/Admin/sites/bulk", payload, token);
                return Json(new { success = true, message = "İşlem başarıyla tamamlandı" });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error in bulk site action");

                // API'den gelen hata mesajını parse et
                if (httpEx.Message.Contains("400") || httpEx.Message.Contains("Bad Request"))
                {
                    if (action == "delete")
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Seçilen şantiyelerden bazılarında çalışan bulunduğu için silme işlemi tamamlanamadı. Önce tüm çalışanları transfer edin veya silin."
                        });
                    }
                }

                return Json(new { success = false, message = "Toplu işlem sırasında hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk site action");
                return Json(new { success = false, message = "Toplu işlem sırasında hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSite(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var site = await _apiService.GetAsync<Models.DTOs.SiteDto>($"api/Admin/sites/{id}", token);
                if (site == null)
                {
                    TempData["ErrorMessage"] = "Şantiye bulunamadı";
                    return RedirectToAction("Sites");
                }

                // Get brands for selection
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;

                // Get available employees (employees without assigned site) + current site employees
                var availableEmployees = await _apiService.GetAsync<List<EmployeeDto>>("api/Admin/employees/available", token) ?? new List<EmployeeDto>();
                var currentSiteEmployees = site.Employees ?? new List<EmployeeDto>();
                
                // Combine and remove duplicates
                var allSelectableEmployees = availableEmployees
                    .Concat(currentSiteEmployees)
                    .GroupBy(e => e.Id)
                    .Select(g => g.First())
                    .ToList();
                    
                ViewBag.AvailableEmployees = allSelectableEmployees;

                var model = new UpdateSiteDto
                {
                    Id = site.Id,
                    Name = site.Name,
                    Address = site.Address,
                    Description = site.Description,
                    IsActive = site.IsActive,
                    BrandIds = site.Brands?.Select(b => b.Id).ToList() ?? new List<int>(),
                    EmployeeIds = site.Employees?.Select(e => e.Id).ToList() ?? new List<int>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading site for edit");
                TempData["ErrorMessage"] = "Şantiye yüklenirken bir hata oluştu";
                return RedirectToAction("Sites");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSite(UpdateSiteDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                // Reload brands and employees for validation error case
                try
                {
                    var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                    ViewBag.Brands = brands;
                    
                    // Get current site info for employee loading
                    var currentSite = await _apiService.GetAsync<Models.DTOs.SiteDto>($"api/Admin/sites/{model.Id}", token);
                    var availableEmployees = await _apiService.GetAsync<List<EmployeeDto>>("api/Admin/employees/available", token) ?? new List<EmployeeDto>();
                    var currentSiteEmployees = currentSite?.Employees ?? new List<EmployeeDto>();
                    
                    var allSelectableEmployees = availableEmployees
                        .Concat(currentSiteEmployees)
                        .GroupBy(e => e.Id)
                        .Select(g => g.First())
                        .ToList();
                        
                    ViewBag.AvailableEmployees = allSelectableEmployees;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading data for validation error case");
                    ViewBag.Brands = new List<BrandDto>();
                    ViewBag.AvailableEmployees = new List<EmployeeDto>();
                }
                return View(model);
            }

            try
            {
                var result = await _apiService.PutAsync<object>($"api/Admin/sites/{model.Id}", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Şantiye başarıyla güncellendi";
                    return RedirectToAction("Sites");
                }

                TempData["ErrorMessage"] = "Şantiye güncellenirken bir hata oluştu";

                // Reload brands and employees for error case
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;
                
                var currentSite = await _apiService.GetAsync<Models.DTOs.SiteDto>($"api/Admin/sites/{model.Id}", token);
                var availableEmployees = await _apiService.GetAsync<List<EmployeeDto>>("api/Admin/employees/available", token) ?? new List<EmployeeDto>();
                var currentSiteEmployees = currentSite?.Employees ?? new List<EmployeeDto>();
                
                var allSelectableEmployees = availableEmployees
                    .Concat(currentSiteEmployees)
                    .GroupBy(e => e.Id)
                    .Select(g => g.First())
                    .ToList();
                    
                ViewBag.AvailableEmployees = allSelectableEmployees;
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site");
                TempData["ErrorMessage"] = "Şantiye güncellenirken bir hata oluştu";

                // Reload brands and employees for error case
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;
                
                var currentSite = await _apiService.GetAsync<Models.DTOs.SiteDto>($"api/Admin/sites/{model.Id}", token);
                var availableEmployees = await _apiService.GetAsync<List<EmployeeDto>>("api/Admin/employees/available", token) ?? new List<EmployeeDto>();
                var currentSiteEmployees = currentSite?.Employees ?? new List<EmployeeDto>();
                
                var allSelectableEmployees = availableEmployees
                    .Concat(currentSiteEmployees)
                    .GroupBy(e => e.Id)
                    .Select(g => g.First())
                    .ToList();
                    
                ViewBag.AvailableEmployees = allSelectableEmployees;
                
                return View(model);
            }
        }

        // Employees Management
        public async Task<IActionResult> Employees()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var employees = await _apiService.GetAsync<List<EmployeeDto>>("api/Admin/employees", token) ?? new List<EmployeeDto>();
            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEmployee()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
            ViewBag.Sites = sites;
            return View(new CreateEmployeeDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sites;
                return View(model);
            }

            try
            {
                // Clean phone number - remove spaces and format to clean number for database storage
                if (!string.IsNullOrEmpty(model.Phone))
                {
                    model.Phone = model.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }

                var result = await _apiService.PostAsync<object>("api/Admin/employees", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Çalışan başarıyla oluşturuldu";
                    return RedirectToAction("Employees");
                }

                TempData["ErrorMessage"] = "Çalışan oluşturulurken bir hata oluştu";
                var sitesForError = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sitesForError;
                return View(model);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error creating employee");
                
                // Parse error message from API
                string errorMessage = "Çalışan oluşturulurken bir hata oluştu";
                
                if (httpEx.Message.Contains("400:"))
                {
                    // Extract the actual error message from "400: message"
                    var parts = httpEx.Message.Split(new[] { "400:" }, 2, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        errorMessage = parts[1].Trim();
                    }
                }
                else if (httpEx.Message.Contains("Bu e-posta adresi zaten kullanılıyor"))
                {
                    errorMessage = "Bu e-posta adresi zaten kullanılıyor. Lütfen farklı bir e-posta adresi kullanın.";
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor");
                }
                else if (httpEx.Message.Contains("Bu telefon numarası zaten kullanılıyor"))
                {
                    errorMessage = "Bu telefon numarası zaten kullanılıyor. Lütfen farklı bir telefon numarası kullanın.";
                    ModelState.AddModelError("Phone", "Bu telefon numarası zaten kullanılıyor");
                }
                else if (httpEx.Message.Contains("Seçilen şantiye bulunamadı"))
                {
                    errorMessage = "Seçilen şantiye bulunamadı. Lütfen geçerli bir şantiye seçin.";
                    ModelState.AddModelError("SiteId", "Geçersiz şantiye seçimi");
                }
                
                TempData["ErrorMessage"] = errorMessage;
                var sitesForError = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sitesForError;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                TempData["ErrorMessage"] = "Çalışan oluşturulurken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                var sitesForError = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sitesForError;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeDetails(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var employee = await _apiService.GetAsync<EmployeeDto>($"api/Admin/employees/{id}", token);
                if (employee == null)
                    return Json(new { success = false, message = "Çalışan bulunamadı" });

                var html = $@"
                    <div class='row'>
                        <div class='col-md-6'>
                            <h6>Kişisel Bilgiler</h6>
                            <p><strong>Ad Soyad:</strong> {employee.FullName}</p>
                            <p><strong>E-posta:</strong> {employee.Email}</p>
                            <p><strong>Telefon:</strong> {employee.Phone}</p>
                            <p><strong>Pozison:</strong> {employee.Position}</p>
                        </div>
                        <div class='col-md-6'>
                            <h6>İş Bilgileri</h6>
                            <p><strong>Şantiye:</strong> {employee.SiteName}</p>
                            <p><strong>Durum:</strong> {(employee.IsActive ? "<span class='badge bg-success'>Aktif</span>" : "<span class='badge bg-danger'>Pasif</span>")}</p>
                            <p><strong>Kayıt Tarihi:</strong> {employee.CreatedDate:dd.MM.yyyy}</p>
                        </div>
                    </div>";

                return Json(new { success = true, html = html });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee details");
                return Json(new { success = false, message = "Detaylar yüklenirken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var employee = await _apiService.GetAsync<EmployeeDto>($"api/Admin/employees/{id}", token);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Çalışan bulunamadı";
                    return RedirectToAction("Employees");
                }

                // Get sites for dropdown
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sites;

                var model = new UpdateEmployeeDto
                {
                    Id = employee.Id,
                    Email=employee.Email,
                    FullName = employee.FullName,
                    Phone = employee.Phone,
                    Position = employee.Position,
                    SiteId = employee.SiteId,
                    IsActive = employee.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee for edit");
                TempData["ErrorMessage"] = "Çalışan yüklenirken bir hata oluştu";
                return RedirectToAction("Employees");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(UpdateEmployeeDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                // Reload sites for validation error case
                try
                {
                    var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                    ViewBag.Sites = sites;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading sites for validation error case");
                    ViewBag.Sites = new List<Models.DTOs.SiteDto>();
                }
                return View(model);
            }

            try
            {
                // Clean phone number
                if (!string.IsNullOrEmpty(model.Phone))
                {
                    model.Phone = model.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }

                var result = await _apiService.PutAsync<object>($"api/Admin/employees/{model.Id}", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Çalışan başarıyla güncellendi";
                    return RedirectToAction("Employees");
                }

                TempData["ErrorMessage"] = "Çalışan güncellenirken bir hata oluştu";

                // Reload sites for error case
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sites;
                
                return View(model);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error updating employee");
                
                // Parse error message from API
                string errorMessage = "Çalışan güncellenirken bir hata oluştu";
                
                if (httpEx.Message.Contains("400:"))
                {
                    var parts = httpEx.Message.Split(new[] { "400:" }, 2, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        errorMessage = parts[1].Trim();
                    }
                }
                else if (httpEx.Message.Contains("Bu telefon numarası"))
                {
                    errorMessage = "Bu telefon numarası başka bir kullanıcı tarafından kullanılıyor.";
                    ModelState.AddModelError("Phone", "Bu telefon numarası zaten kullanılıyor");
                }
                else if (httpEx.Message.Contains("Seçilen şantiye bulunamadı"))
                {
                    errorMessage = "Seçilen şantiye bulunamadı. Lütfen geçerli bir şantiye seçin.";
                    ModelState.AddModelError("SiteId", "Geçersiz şantiye seçimi");
                }
                
                TempData["ErrorMessage"] = errorMessage;
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sites;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                TempData["ErrorMessage"] = "Çalışan güncellenirken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                ViewBag.Sites = sites;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var result = await _apiService.DeleteAsync($"api/Admin/employees/{id}", token);
                return Json(new { success = true, message = "Çalışan başarıyla silindi" });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error deleting employee");

                // API'den gelen hata mesajını parse et
                string errorMessage = "Çalışan silinirken bir hata oluştu";
                
                if (httpEx.Message.Contains("400:"))
                {
                    var parts = httpEx.Message.Split(new[] { "400:" }, 2, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        errorMessage = parts[1].Trim();
                    }
                }
                else if (httpEx.Message.Contains("404") || httpEx.Message.Contains("Not Found"))
                {
                    errorMessage = "Çalışan bulunamadı";
                }
                else if (httpEx.Message.Contains("aktif talepleri"))
                {
                    errorMessage = "Bu çalışanın aktif talepleri bulunduğu için silinemez. Önce tüm taleplerini tamamlayın veya iptal edin.";
                }

                return Json(new { success = false, message = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                return Json(new { success = false, message = "Çalışan silinirken beklenmeyen bir hata oluştu" });
            }
        }

        // Suppliers Management
        public async Task<IActionResult> Suppliers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var suppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers", token) ?? new List<SupplierDto>();
            return View(suppliers);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSupplier()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            return View(new CreateSupplierDto());
        }

        [HttpPost]  
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(CreateSupplierDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Clean phone number - remove spaces and format to clean number for database storage
                if (!string.IsNullOrEmpty(model.Phone))
                {
                    model.Phone = model.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }

                var result = await _apiService.PostAsync<object>("api/Admin/suppliers", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Tedarikçi başarıyla oluşturuldu";
                    return RedirectToAction("Suppliers");
                }

                TempData["ErrorMessage"] = "Tedarikçi oluşturulurken bir hata oluştu";
                return View(model);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error creating supplier");
                
                // Parse error message from API
                string errorMessage = "Tedarikçi oluşturulurken bir hata oluştu";
                
                if (httpEx.Message.Contains("400:"))
                {
                    // Extract the actual error message from "400: message"
                    var parts = httpEx.Message.Split(new[] { "400:" }, 2, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        errorMessage = parts[1].Trim();
                    }
                }
                else if (httpEx.Message.Contains("Bu e-posta adresi zaten kullanılıyor"))
                {
                    errorMessage = "Bu e-posta adresi zaten kullanılıyor. Lütfen farklı bir e-posta adresi kullanın.";
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor");
                }
                else if (httpEx.Message.Contains("Bu telefon numarası zaten kullanılıyor"))
                {
                    errorMessage = "Bu telefon numarası zaten kullanılıyor. Lütfen farklı bir telefon numarası kullanın.";
                    ModelState.AddModelError("Phone", "Bu telefon numarası zaten kullanılıyor");
                }
                
                TempData["ErrorMessage"] = errorMessage;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                TempData["ErrorMessage"] = "Tedarikçi oluşturulurken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSupplier(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var supplier = await _apiService.GetAsync<SupplierDto>($"api/Admin/suppliers/{id}", token);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Tedarikçi bulunamadı";
                    return RedirectToAction("Suppliers");
                }

                var model = new UpdateSupplierDto
                {
                    Id = supplier.Id,
                    FullName = supplier.User?.FullName ?? "",
                    Email = supplier.User?.Email ?? "",
                    Phone = supplier.User?.Phone ?? "",
                    CompanyName = supplier.CompanyName,
                    TaxNumber = supplier.TaxNumber,
                    Address = supplier.Address,
                    IsActive = supplier.User?.IsActive ?? false
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier for edit");
                TempData["ErrorMessage"] = "Tedarikçi yüklenirken bir hata oluştu";
                return RedirectToAction("Suppliers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(UpdateSupplierDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Clean phone number
                if (!string.IsNullOrEmpty(model.Phone))
                {
                    model.Phone = model.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }

                var result = await _apiService.PutAsync<object>($"api/Admin/suppliers/{model.Id}", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Tedarikçi başarıyla güncellendi";
                    return RedirectToAction("Suppliers");
                }

                TempData["ErrorMessage"] = "Tedarikçi güncellenirken bir hata oluştu";
                return View(model);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error updating supplier");

                string errorMessage = "Tedarikçi güncellenirken bir hata oluştu";

                if (httpEx.Message.Contains("400:"))
                {
                    var parts = httpEx.Message.Split(new[] { "400:" }, 2, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        errorMessage = parts[1].Trim();
                    }
                }
                else if (httpEx.Message.Contains("Bu telefon numarası"))
                {
                    errorMessage = "Bu telefon numarası başka bir kullanıcı tarafından kullanılıyor.";
                    ModelState.AddModelError("Phone", "Bu telefon numarası zaten kullanılıyor");
                }

                TempData["ErrorMessage"] = errorMessage;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier");
                TempData["ErrorMessage"] = "Tedarikçi güncellenirken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierDetails(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var supplier = await _apiService.GetAsync<SupplierDto>($"api/Admin/suppliers/{id}", token);
                if (supplier == null)
                    return Json(new { success = false, message = "Tedarikçi bulunamadı" });

                var html = $@"
                    <div class='row'>
                        <div class='col-md-6'>
                            <h6>Kişisel Bilgiler</h6>
                            <p><strong>Ad Soyad:</strong> {supplier.User?.FullName}</p>
                            <p><strong>E-posta:</strong> {supplier.User?.Email}</p>
                            <p><strong>Telefon:</strong> {supplier.User?.Phone}</p>
                        </div>
                        <div class='col-md-6'>
                            <h6>Şirket Bilgileri</h6>
                            <p><strong>Şirket:</strong> {supplier.CompanyName}</p>
                            <p><strong>Vergi No:</strong> {supplier.TaxNumber}</p>
                            <p><strong>Adres:</strong> {supplier.Address}</p>
                            <p><strong>Durum:</strong> {GetSupplierStatusBadge(supplier.Status)}</p>
                        </div>
                    </div>";

                return Json(new { success = true, html = html });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier details");
                return Json(new { success = false, message = "Detaylar yüklenirken hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var result = await _apiService.DeleteAsync($"api/Admin/suppliers/{id}", token);
                return Json(new { success = true, message = "Tedarikçi başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier");
                return Json(new { success = false, message = "Tedarikçi silinirken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetApprovedSuppliers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var suppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers/approved", token) ?? new List<SupplierDto>();
                return Json(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approved suppliers");
                return Json(new { success = false, message = "Tedarikçiler yüklenirken hata oluştu" });
            }
        }

        // Requests Management
        public async Task<IActionResult> Requests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var requests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();
            var model = new SantiyeTalepWebUI.Models.DTOs.RequestListViewModel
            {
                Requests = requests
            };

            return View(model);
        }

        public async Task<IActionResult> PendingRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var requests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests/pending", token) ?? new List<RequestDto>();
            var model = new SantiyeTalepWebUI.Models.DTOs.RequestListViewModel
            {
                Requests = requests
            };

            return View("Requests", model);
        }

        public async Task<IActionResult> CompletedRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var requests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests/completed", token) ?? new List<RequestDto>();
            var model = new SantiyeTalepWebUI.Models.DTOs.RequestListViewModel
            {
                Requests = requests
            };

            return View("Requests", model);
        }

        // Offers Management
        public async Task<IActionResult> Offers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var offers = await _apiService.GetAsync<List<OfferDto>>("api/Admin/offers", token) ?? new List<OfferDto>();
            var model = new SantiyeTalepWebUI.Models.DTOs.OfferListViewModel
            {
                Offers = offers
            };

            return View(model);
        }

        public async Task<IActionResult> PendingOffers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var offers = await _apiService.GetAsync<List<OfferDto>>("api/Admin/offers/pending", token) ?? new List<OfferDto>();
            var model = new SantiyeTalepWebUI.Models.DTOs.OfferListViewModel
            {
                Offers = offers
            };

            return View("Offers", model);
        }

        public async Task<IActionResult> ApprovedOffers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var offers = await _apiService.GetAsync<List<OfferDto>>("api/Admin/offers/approved", token) ?? new List<OfferDto>();
            var model = new SantiyeTalepWebUI.Models.DTOs.OfferListViewModel
            {
                Offers = offers
            };

            return View("Offers", model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveOffer(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var result = await _apiService.PutAsync<object>($"api/Admin/offers/{id}/approve", new { }, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Teklif onaylandı";
                }
                else
                {
                    TempData["ErrorMessage"] = "Teklif onaylanırken bir hata oluştu";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving offer");
                TempData["ErrorMessage"] = "Teklif onaylanırken bir hata oluştu";
            }

            return RedirectToAction("Offers");
        }

        [HttpPost]
        public async Task<IActionResult> RejectOffer(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var result = await _apiService.PutAsync<object>($"api/Admin/offers/{id}/reject", new { }, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Teklif reddedildi";
                }
                else
                {
                    TempData["ErrorMessage"] = "Teklif reddedilirken bir hata oluştu";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting offer");
                TempData["ErrorMessage"] = "Teklif reddedilirken bir hata oluştu";
            }

            return RedirectToAction("Offers");
        }

        [HttpPost]
        public async Task<IActionResult> BulkOfferAction([FromBody] BulkOfferActionModel model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var apiModel = new
                {
                    OfferIds = model.OfferIds,
                    Action = model.Action
                };

                var result = await _apiService.PostAsync<object>("api/Admin/offers/bulk", apiModel, token);
                
                if (result != null)
                {
                    var actionText = model.Action.ToLower() == "approve" ? "onayandı" : "reddedildi";
                    return Json(new { success = true, message = $"{model.OfferIds.Count} teklif başarıyla {actionText}" });
                }

                return Json(new { success = false, message = "Toplu işlem sırasında bir hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk offer action");
                return Json(new { success = false, message = "Toplu işlem sırasında bir hata oluştu: " + ex.Message });
            }
        }

        // Export Actions
        public async Task<IActionResult> ExportSitesToExcel()
        {
            // TODO: Implement Excel export
            TempData["ErrorMessage"] = "Excel çıktısı henüz implementlenmedi";
            return RedirectToAction("Sites");
        }

        public async Task<IActionResult> ExportSitesToPDF()
        {
            // TODO: Implement PDF export
            TempData["ErrorMessage"] = "PDF çıktısı henüz implementlenmedi";
            return RedirectToAction("Sites");
        }

        // Reports
        public async Task<IActionResult> RequestReports()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            // TODO: Implement request reports
            return View();
        }

        public async Task<IActionResult> SupplierReports()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            // TODO: Implement supplier reports
            return View();
        }

        public async Task<IActionResult> CostReports()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            // TODO: Implement cost reports
            return View();
        }

        // System Settings
        public async Task<IActionResult> Users()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            // TODO: Implement user management
            return View();
        }

        public async Task<IActionResult> SystemSettings()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            // TODO: Implement system settings
            return View();
        }

        public async Task<IActionResult> BackupRestore()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            // TODO: Implement backup restore
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var request = await _apiService.GetAsync<RequestDto>($"api/Request/{id}", token);
                if (request == null)
                    return Json(new { success = false, message = "Talep bulunamadı" });

                // Build HTML response with enhanced styling
                var html = $@"
                    <div class='row'>
                        <div class='col-md-8'>
                            <div class='card h-100'>
                                <div class='card-header'>
                                    <h6 class='card-title mb-0'>
                                        <i class='ri-file-list-3-line text-primary me-2'></i>
                                        Talep Bilgileri
                                    </h6>
                                </div>
                                <div class='card-body'>
                                    <div class='row g-3'>
                                        <div class='col-12'>
                                            <label class='form-label fw-semibold text-muted'>Talep ID</label>
                                            <p class='mb-0'>#{request.Id}</p>
                                        </div>
                                        <div class='col-12'>
                                            <label class='form-label fw-semibold text-muted'>Ürün Açıklaması</label>
                                            <p class='mb-0'>{request.ProductDescription}</p>
                                        </div>
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Miktar</label>
                                            <p class='mb-0>
                                                <span class='badge bg-primary'>{request.Quantity}</span>
                                            </p>
                                        </div>
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Durum</label>
                                            <div>
                                                {GetStatusBadge(request.Status)}
                                            </div>
                                        </div>
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Teslim Tipi</label>
                                            <p class='mb-0'>{GetDeliveryTypeText(request.DeliveryType)}</p>
                                        </div>
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Talep Tarihi</label>
                                            <div>
                                                <i class='ri-calendar-line text-info me-2'></i>
                                                {request.RequestDate:dd.MM.yyyy HH:mm}
                                            </div>
                                        </div>
                                        {(!string.IsNullOrEmpty(request.Description) ? $@"
                                        <div class='col-12'>
                                            <label class='form-label fw-semibold text-muted'>Açıklama</label>
                                            <p class='mb-0'>{request.Description}</p>
                                        </div>" : "")}
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Çalışan</label>
                                            <div class='d-flex align-items-center'>
                                                <div class='avatar-xs me-2'>
                                                    <div class='avatar-title bg-primary-subtle text-primary rounded-circle'>
                                                        {(request.EmployeeName?.Substring(0, 1).ToUpper() ?? "?")}
                                                    </div>
                                                </div>
                                                <span>{request.EmployeeName}</span>
                                            </div>
                                        </div>
                                        <div class='col-6'>
                                            <label class='form-label fw-semibold text-muted'>Şantiye</label>
                                            <div>
                                                <span class='badge bg-info'>{request.SiteName}</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class='col-md-4'>
                            <div class='card h-100'>
                                <div class='card-header'>
                                    <h6 class='card-title mb-0'>
                                        <i class='ri-price-tag-3-line text-success me-2'></i>
                                        Teklif Özeti
                                    </h6>
                                </div>
                                <div class='card-body'>
                                    <div class='text-center mb-3'>
                                        <div class='avatar-lg mx-auto mb-2'>
                                            <div class='avatar-title bg-success-subtle text-success rounded-circle'>
                                                <i class='ri-price-tag-line display-6'></i>
                                            </div>
                                        </div>
                                        <h4 class='mb-1'>{request.Offers?.Count ?? 0}</h4>
                                        <p class='text-muted mb-0'>Toplam Teklif</p>
                                    </div>
                                    {(request.Offers?.Any() == true ? $@"
                                    <div class='mt-3'>
                                        <div class='list-group list-group-flush'>
                                            {string.Join("", request.Offers.Take(3).Select(offer => $@"
                                            <div class='list-group-item px-0'>
                                                <div class='d-flex justify-content-between align-items-start'>
                                                    <div class='flex-grow-1'>
                                                        <h6 class='mb-1'>{offer.SupplierName}</h6>
                                                        <small class='text-muted'>{offer.CompanyName}</small>
                                                        <div class='mt-1'>
                                                            <span class='badge bg-info-subtle text-info'>{offer.Brand}</span>
                                                        </div>
                                                    </div>
                                                    <div class='text-end'>
                                                        <div class='mb-1'>
                                                            <span class='fw-bold'>{offer.CurrencySymbol}{offer.FinalPrice:N2}</span>
                                                            <div><small class='text-muted'>{offer.CurrencyName}</small></div>
                                                        </div>
                                                        <div><small class='text-muted'>{offer.DeliveryDays} gün teslimat</small></div>
                                                        <div class='mt-1'>
                                                            {GetOfferStatusBadge(offer.Status)}
                                                        </div>
                                                    </div>
                                                </div>
                                                {(offer.Discount > 0 ? $@"
                                                <div class='mt-2 pt-2 border-top'>
                                                    <small class='text-muted'>
                                                        Liste: {offer.CurrencySymbol}{offer.Price:N2} | 
                                                        İskonto: %{offer.Discount} | 
                                                        Net: {offer.CurrencySymbol}{offer.FinalPrice:N2}
                                                    </small>
                                                </div>" : "")}
                                            </div>"))}
                                        </div>
                                        {(request.Offers.Count > 3 ? $@"
                                        <div class='text-center mt-2'>
                                            <small class='text-muted'>ve {request.Offers.Count - 3} teklif daha...</small>
                                        </div>" : "")}
                                        <div class='text-center mt-3'>
                                            <a href='/Admin/Offers?offerId={request.Offers.First().Id}' class='btn btn-sm btn-outline-primary'>
                                                <i class='ri-eye-line me-1'></i>Tüm Teklifleri Görüntüle
                                            </a>
                                        </div>
                                    </div>" : @"
                                    <div class='text-center py-3'>
                                        <i class='ri-price-tag-line display-6 text-muted mb-2'></i>
                                        <p class='text-muted mb-0'>Henüz teklif yok</p>
                                    </div>")}
                                </div>
                            </div>
                        </div>
                    </div>";

                return Json(new { success = true, html = html });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request details");
                return Json(new { success = false, message = "Detaylar yüklenirken hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRequestStatus([FromBody] ChangeRequestStatusDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var payload = new { status = model.NewStatus };
                var result = await _apiService.PutAsync<object>($"api/Admin/requests/{model.RequestId}/status", payload, token);
                return Json(new { success = true, message = "Talep durumu güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing request status");
                return Json(new { success = false, message = "Durum güncellenirken hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var result = await _apiService.DeleteAsync($"api/Admin/requests/{id}", token);
                return Json(new { success = true, message = "Talep başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting request");
                return Json(new { success = false, message = "Talep silinirken hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkRequestAction([FromBody] BulkRequestActionDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var payload = new { requestIds = model.RequestIds, action = model.Action };
                var result = await _apiService.PostAsync<object>("api/Admin/requests/bulk", payload, token);
                return Json(new { success = true, message = "Toplu işlem başarıyla tamamlandı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk request action");
                return Json(new { success = false, message = "Toplu işlem sırasında hata oluştu" });
            }
        }

        public async Task<IActionResult> ExportRequestsToExcel()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var requests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();

                // TODO: Implement Excel export functionality
                TempData["InfoMessage"] = "Excel export özelliği yakında eklenecek";
                return RedirectToAction("Requests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting requests to Excel");
                TempData["ErrorMessage"] = "Excel export sırasında hata oluştu";
                return RedirectToAction("Requests");
            }
        }

        public async Task<IActionResult> ExportRequestsToPDF()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var requests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();

                // TODO: Implement PDF export functionality
                TempData["InfoMessage"] = "PDF export özelliği yakında eklenecek";
                return RedirectToAction("Requests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting requests to PDF");
                TempData["ErrorMessage"] = "PDF export sırasında hata oluştu";
                return RedirectToAction("Requests");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendRequestsToSuppliers([FromBody] SendRequestsToSuppliersModel model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var apiModel = new
                {
                    RequestIds = model.RequestIds,
                    SupplierIds = model.SupplierIds
                };

                var result = await _apiService.PostAsync<object>("api/Admin/requests/send-to-suppliers", apiModel, token);
                
                if (result != null)
                {
                    return Json(new { success = true, message = "Talepler başarıyla tedarikçilere gönderildi" });
                }

                return Json(new { success = false, message = "Talepler gönderilirken bir hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending requests to suppliers");
                return Json(new { success = false, message = "Talepler gönderilirken bir hata oluştu: " + ex.Message });
            }
        }

        private string GetStatusBadge(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Open => "<span class='badge bg-warning'>Açık</span>",
                RequestStatus.InProgress => "<span class='badge bg-info'>İşlemde</span>",
                RequestStatus.Completed => "<span class='badge bg-success'>Tamamlandı</span>",
                RequestStatus.Cancelled => "<span class='badge bg-danger'>İptal</span>",
                _ => "<span class='badge bg-secondary'>Bilinmeyen</span>"
            };
        }

        private string GetDeliveryTypeText(DeliveryType deliveryType)
        {
            return deliveryType switch
            {
                DeliveryType.TodayPickup => "Bugün araç gönderip aldıracağım",
                DeliveryType.SameDayDelivery => "Gün içi siz sevk edin",
                DeliveryType.NextDayDelivery => "Yarın siz sevk edin",
                DeliveryType.BusinessDays1to2 => "1-2 iş günü",
                _ => deliveryType.ToString()
            };
        }

        private string GetOfferStatusBadge(OfferStatus status)
        {
            return status switch
            {
                OfferStatus.Pending => "<span class='badge bg-warning'>Bekleyen</span>",
                OfferStatus.Approved => "<span class='badge bg-success'>Onaylandı</span>",
                OfferStatus.Rejected => "<span class='badge bg-danger'>Reddedildi</span>",
                _ => "<span class='badge bg-secondary'>Bilinmeyen</span>"
            };
        }

        private string GetSupplierStatusBadge(SupplierStatus status)
        {
            return status switch
            {
                SupplierStatus.Pending => "<span class='badge bg-warning'>Onay Bekliyor</span>",
                SupplierStatus.Approved => "<span class='badge bg-success'>Onaylı</span>",
                SupplierStatus.Rejected => "<span class='badge bg-danger'>Redd edildi</span>",
                _ => "<span class='badge bg-secondary'>Bilinmeyen</span>"
            };
        }

        [HttpGet]
        public async Task<IActionResult> CheckNewRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var recentRequests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();
                var allSuppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers", token) ?? new List<SupplierDto>();
                var pendingOffers = await _apiService.GetAsync<List<OfferDto>>("api/Admin/offers/pending", token) ?? new List<OfferDto>();
                
                // Filter pending suppliers from all suppliers
                var pendingSuppliers = allSuppliers.Where(s => s.Status == SupplierStatus.Pending).ToList();
                
                // Count new requests from today
                var today = DateTime.Today;
                var newRequestsToday = recentRequests.Count(r => r.RequestDate.Date == today);
                
                return Json(new { 
                    success = true, 
                    data = new {
                        newRequestsToday = newRequestsToday,
                        pendingSuppliersCount = pendingSuppliers.Count,
                        pendingOffersCount = pendingOffers.Count,
                        hasNewContent = newRequestsToday > 0 || pendingSuppliers.Any() || pendingOffers.Any()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking new requests for admin");
                return Json(new { success = false, message = "Yeni talepler kontrol edilirken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestStatusData(string period = "month")
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var allRequests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();
                
                // Filtre tarih aralığını hesapla
                DateTime startDate;
                var now = DateTime.Now;

                switch (period.ToLower())
                {
                    case "today":
                        startDate = DateTime.Today;
                        break;
                    case "week":
                        startDate = now.AddDays(-7);
                        break;
                    case "month":
                        startDate = now.AddMonths(-1);
                        break;
                    case "3months":
                        startDate = now.AddMonths(-3);
                        break;
                    case "year":
                        startDate = now.AddYears(-1);
                        break;
                    default:
                        startDate = now.AddMonths(-1);
                        break;
                }

                // Tarih aralığına göre filtrele
                var filteredRequests = allRequests.Where(r => r.RequestDate >= startDate).ToList();

                // Duruma göre say
                var openCount = filteredRequests.Count(r => r.Status == RequestStatus.Open);
                var inProgressCount = filteredRequests.Count(r => r.Status == RequestStatus.InProgress);
                var completedCount = filteredRequests.Count(r => r.Status == RequestStatus.Completed);
                var cancelledCount = filteredRequests.Count(r => r.Status == RequestStatus.Cancelled);

                return Json(new { 
                    success = true, 
                    openCount = openCount,
                    inProgressCount = inProgressCount,
                    completedCount = completedCount,
                    cancelledCount = cancelledCount,
                    total = filteredRequests.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request status data");
                return Json(new { success = false, message = "Grafik verileri yüklenirken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSitePerformanceData(string period = "1month")
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var performanceData = await _apiService.GetAsync<dynamic>($"api/Admin/site-performance?period={period}", token);
                
                if (performanceData != null)
                {
                    return Json(new { success = true, data = performanceData });
                }

                return Json(new { success = false, message = "Şantiye performans verileri alınamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site performance data");
                return Json(new { success = false, message = "Şantiye performans verileri yüklenirken hata oluştu" });
            }
        }

        // CreateRequest Actions
        [HttpGet]
        public async Task<IActionResult> CreateRequest()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            return View(new CreateRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(CreateRequestViewModel model)
        {
            _logger.LogInformation("CreateRequest POST called");
            _logger.LogInformation("Model - SiteId: {SiteId}, EmployeeId: {EmployeeId}, Product: {Product}, Quantity: {Quantity}",
                model.SiteId, model.EmployeeId, model.ProductDescription, model.Quantity);

            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found for CreateRequest");
                return RedirectToAction("Login", "Account");
            }

            // Validate Admin-specific fields
            if (!model.SiteId.HasValue || model.SiteId.Value <= 0)
            {
                ModelState.AddModelError("SiteId", "Lütfen bir şantiye seçin");
                _logger.LogWarning("SiteId is missing or invalid: {SiteId}", model.SiteId);
            }

            if (!model.EmployeeId.HasValue || model.EmployeeId.Value <= 0)
            {
                ModelState.AddModelError("EmployeeId", "Lütfen bir çalışan seçin");
                _logger.LogWarning("EmployeeId is missing or invalid: {EmployeeId}", model.EmployeeId);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("ModelState Error - Key: {Key}, Errors: {Errors}",
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
                return View(model);
            }

            try
            {
                // Admin için özel DTO oluştur
                var createDto = new
                {
                    SiteId = model.SiteId.Value,
                    EmployeeId = model.EmployeeId.Value,
                    ProductDescription = model.ProductDescription?.Trim(),
                    Quantity = model.Quantity,
                    DeliveryType = model.DeliveryType,
                    Description = model.Description?.Trim()
                };

                _logger.LogInformation("Sending request to API: {Request}",
                    System.Text.Json.JsonSerializer.Serialize(createDto));

                // Admin endpoint'ini kullan
                var result = await _apiService.PostAsync<object>("api/Request", createDto, token);

                _logger.LogInformation("API response received: {Result}",
                    result != null ? "Success" : "Null");

                if (result != null)
                {
                    TempData["SuccessMessage"] = "Talep başarıyla oluşturuldu";
                    return RedirectToAction("Requests");
                }

                TempData["ErrorMessage"] = "Talep oluşturulurken bir hata oluştu";
                _logger.LogWarning("API returned null result");
                return View(model);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error creating request");
                _logger.LogError("Full HTTP exception message: {Message}", httpEx.Message);

                string errorMessage = "Talep oluşturulurken bir hata oluştu";

                if (httpEx.Message.Contains("400"))
                {
                    // API'den gelen detaylı hata mesajını almaya çalış
                    try
                    {
                        // HttpRequestException'dan response body'yi alamayız, 
                        // bu yüzden genel bir mesaj gösterelim
                        errorMessage = "Gönderilen veriler geçersiz. Lütfen tüm alanları kontrol edin.";

                        // Log'da daha detaylı bilgi olsun
                        _logger.LogError("400 Bad Request details: {Details}", httpEx.Message);
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                }
                else if (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
                {
                    errorMessage = "Bu işlem için yetkiniz bulunmuyor";
                }
                else if (httpEx.Message.Contains("401") || httpEx.Message.Contains("Unauthorized"))
                {
                    errorMessage = "Oturum süreniz dolmuş, lütfen tekrar giriş yapın";
                    return RedirectToAction("Login", "Account");
                }

                TempData["ErrorMessage"] = errorMessage;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request");
                TempData["ErrorMessage"] = "Talep oluşturulurken beklenmeyen bir hata oluştu: " + ex.Message;
                return View(model);
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateRequestFromExcel()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var form = await Request.ReadFormAsync();
                var excelFile = form.Files["ExcelFile"];
                var siteId = form["SiteId"].ToString();
                var employeeId = form["EmployeeId"].ToString();
                var supplierIds = form["SupplierIds"].ToList();

                if (excelFile == null || excelFile.Length == 0)
                    return Json(new { success = false, message = "Excel dosyası seçilmedi" });

                if (string.IsNullOrEmpty(siteId) || string.IsNullOrEmpty(employeeId))
                    return Json(new { success = false, message = "Şantiye ve çalışan seçimi zorunludur" });

                if (!supplierIds.Any())
                    return Json(new { success = false, message = "En az bir tedarikçi seçmelisiniz" });

                // Excel dosyasını multipart form data olarak gönder
                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(excelFile.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(excelFile.ContentType);
                content.Add(fileContent, "ExcelFile", excelFile.FileName);
                content.Add(new StringContent(siteId), "SiteId");
                content.Add(new StringContent(employeeId), "EmployeeId");
                
                foreach (var supplierId in supplierIds)
                {
                    content.Add(new StringContent(supplierId), "SupplierIds");
                }

                // API'ye gönder
                var result = await _apiService.PostMultipartAsync<object>("api/Admin/requests/create-from-excel", content, token);
                
                if (result != null)
                {
                    return Json(new { success = true, message = "Excel dosyası başarıyla yuedlendi ve tedarikçilere gönderildi" });
                }

                return Json(new { success = false, message = "Excel yüklenirken bir hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request from Excel");
                return Json(new { success = false, message = "Excel yüklenirken bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSites()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new List<object>());

            try
            {
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                var result = sites.Select(s => new { id = s.Id, name = s.Name }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sites");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesBySite(int siteId)
        {
            _logger.LogInformation("GetEmployeesBySite called with siteId: {SiteId}", siteId);
            
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found for GetEmployeesBySite request");
                return Json(new List<object>());
            }

            try
            {
                _logger.LogInformation("Fetching employees from API for site: {SiteId}", siteId);
                
                // Backend API endpoint: /api/Admin/sites/{id}/employees
                var employees = await _apiService.GetAsync<List<EmployeeDto>>($"api/Admin/sites/{siteId}/employees", token) ?? new List<EmployeeDto>();
                
                _logger.LogInformation("Received {Count} employees from API for site {SiteId}", employees.Count, siteId);
                
                var result = employees.Select(e => new { 
                    id = e.Id, 
                    fullName = e.FullName, 
                    position = e.Position 
                }).ToList();
                
                _logger.LogInformation("Returning {Count} employees to frontend", result.Count);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by site {SiteId}", siteId);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string query, int? siteId)
        {
            _logger.LogInformation("SearchProducts called - Query: {Query}, SiteId: {SiteId}", query, siteId);
            
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
                // Build search URL with brand filtering based on selected site
                var searchUrl = $"api/searchapi?query={Uri.EscapeDataString(query)}";
                
                // If a site is selected, get its brands for filtering
                if (siteId.HasValue && siteId.Value > 0)
                {
                    _logger.LogInformation("Fetching brands for site: {SiteId}", siteId.Value);
                    
                    var site = await _apiService.GetAsync<Models.DTOs.SiteDto>($"api/Admin/sites/{siteId.Value}", token);
                    
                    if (site?.Brands != null && site.Brands.Any())
                    {
                        var brandIds = string.Join(",", site.Brands.Select(b => b.Id));
                        searchUrl += $"&brandIds={brandIds}";
                        
                        _logger.LogInformation("Applying brand filter for site {SiteName}: {BrandNames}", 
                            site.Name, string.Join(", ", site.Brands.Select(b => b.Name)));
                    }
                }
                
                _logger.LogInformation("Calling search API: {Url}", searchUrl);
                
                var products = await _apiService.GetAsync<List<Models.ViewModels.ProductDto>>(searchUrl, token) ?? new List<Models.ViewModels.ProductDto>();
                
                _logger.LogInformation("Search returned {Count} products", products.Count);
                
                return Json(new { 
                    success = true, 
                    data = products
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return Json(new { success = false, message = "Ürün arama sırasında hata oluştu: " + ex.Message });
            }
        }

        /// <summary>
        /// Excel yönetim sayfası
        /// </summary>
        [HttpGet]
        public IActionResult ExcelManagement(int? requestId = null)
        {
            // requestId parametresi bildirimden gelirse highlight için kullanılacak
            if (requestId.HasValue)
            {
                ViewBag.HighlightRequestId = requestId.Value;
            }
            return View();
        }

        [HttpGet]
        public IActionResult SupplierExcelManagement(int? requestId = null)
        {
            // requestId parametresi bildirimdan gelirse highlight için kullanılacak
            if (requestId.HasValue)
            {
                ViewBag.HighlightRequestId = requestId.Value;
            }
            return View();
        }

       //Notification Management Methods
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
                return Json(new { success = false, message = "Bildirim güncellenirken hata oluştu" });
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

    public class BulkRequestActionDto
    {
        public List<int> RequestIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
    }

    public class SendRequestsToSuppliersModel
    {
        public List<int> RequestIds { get; set; } = new();
        public List<int> SupplierIds { get; } = new();
    }

    public class ChangeRequestStatusDto
    {
        public int RequestId { get; set; }
        public int NewStatus { get; set; }
    }

    public class BulkOfferActionModel
    {
        public List<int> OfferIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
    }
}