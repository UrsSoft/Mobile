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
    public class AdminController : Controller
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
                var pendingSuppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers/pending", token) ?? new List<SupplierDto>();
                var recentRequests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();
                var sites = await _apiService.GetAsync<List<Models.DTOs.SiteDto>>("api/Admin/sites", token) ?? new List<Models.DTOs.SiteDto>();
                var employees = await _apiService.GetAsync<List<EmployeeDto>>("api/Admin/employees", token) ?? new List<EmployeeDto>();

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
                        dashboardStats.PendingSuppliers = GetIntValue(statsDict, "pendingSuppliers");
                        // Employee count from actual employees list
                        dashboardStats.TotalUsers = employees.Count;
                    }
                }

                var model = new AdminDashboardViewModel
                {
                    Stats = dashboardStats,
                    PendingSuppliers = pendingSuppliers,
                    RecentRequests = recentRequests.Take(10).ToList(),
                    Sites = sites,
                    Employees = employees.Take(10).ToList()
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
                _logger.LogError(ex, "Error loading brands for site creation");
                TempData["ErrorMessage"] = "Markalar yüklenirken bir hata oluştu";
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
                    _logger.LogError(ex, "Error loading brands for validation error case");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site");
                TempData["ErrorMessage"] = "Şantiye oluşturulurken bir hata oluştu";
                
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
                                <th><i class='ri-briefcase-line me-1'></i>Pozisyon</th>
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
                    return Json(new { 
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
                        return Json(new { 
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

                var model = new UpdateSiteDto
                {
                    Id = site.Id,
                    Name = site.Name,
                    Address = site.Address,
                    Description = site.Description,
                    IsActive = site.IsActive,
                    BrandIds = site.Brands?.Select(b => b.Id).ToList() ?? new List<int>()
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
                // Reload brands for validation error case
                try
                {
                    var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                    ViewBag.Brands = brands;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading brands for validation error case");
                    ViewBag.Brands = new List<BrandDto>();
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
                
                // Reload brands for error case
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site");
                TempData["ErrorMessage"] = "Şantiye güncellenirken bir hata oluştu";
                
                // Reload brands for error case
                var brands = await _apiService.GetAsync<List<BrandDto>>("api/Admin/brands", token) ?? new List<BrandDto>();
                ViewBag.Brands = brands;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                TempData["ErrorMessage"] = "Çalışan oluşturulurken bir hata oluştu";
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
                            <p><strong>Pozisyon:</strong> {employee.Position}</p>
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

        [HttpPost]
        public async Task<IActionResult> ToggleEmployeeStatus(int id, bool isActive)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuş" });

            try
            {
                var payload = new { isActive = isActive };
                var result = await _apiService.PutAsync<object>($"api/Admin/employees/{id}/status", payload, token);
                return Json(new { success = true, message = "Durum başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling employee status");
                return Json(new { success = false, message = "Durum güncellenirken hata oluştu" });
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                return Json(new { success = false, message = "Çalışan silinirken hata oluştu" });
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

        public async Task<IActionResult> PendingSuppliers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var suppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers/pending", token) ?? new List<SupplierDto>();
            return View("Suppliers", suppliers);
        }

        public async Task<IActionResult> ApprovedSuppliers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var suppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers/approved", token) ?? new List<SupplierDto>();
            return View("Suppliers", suppliers);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveSupplier(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var result = await _apiService.PutAsync<object>($"api/Admin/suppliers/{id}/approve", new { }, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Tedarikçi başarıyla onaylandı";
                }
                else
                {
                    TempData["ErrorMessage"] = "Tedarikçi onaylanırken bir hata oluştu";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving supplier");
                TempData["ErrorMessage"] = "Tedarikçi onaylanırken bir hata oluştu";
            }

            return RedirectToAction("Suppliers");
        }

        [HttpPost]
        public async Task<IActionResult> RejectSupplier(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var result = await _apiService.PutAsync<object>($"api/Admin/suppliers/{id}/reject", new { }, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Tedarikçi reddedildi";
                }
                else
                {
                    TempData["ErrorMessage"] = "Tedarikçi reddedilirken bir hata oluştu";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting supplier");
                TempData["ErrorMessage"] = "Tedarikçi reddedilirken bir hata oluştu";
            }

            return RedirectToAction("Suppliers");
        }

        // Requests Management
        public async Task<IActionResult> Requests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var requests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();
            var model = new RequestListViewModel
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
            var model = new RequestListViewModel
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
            var model = new RequestListViewModel
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
            var model = new OfferListViewModel
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
            var model = new OfferListViewModel
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
            var model = new OfferListViewModel
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
    }
}