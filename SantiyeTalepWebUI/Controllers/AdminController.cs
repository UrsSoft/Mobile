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
                var stats = new DashboardStats();
                var pendingSuppliers = await _apiService.GetAsync<List<SupplierDto>>("api/Admin/suppliers/pending", token) ?? new List<SupplierDto>();
                var recentRequests = await _apiService.GetAsync<List<RequestDto>>("api/Admin/requests", token) ?? new List<RequestDto>();
                var sites = await _apiService.GetAsync<List<SiteDto>>("api/Admin/sites", token) ?? new List<SiteDto>();

                var model = new AdminDashboardViewModel
                {
                    Stats = stats,
                    PendingSuppliers = pendingSuppliers,
                    RecentRequests = recentRequests.Take(10).ToList(),
                    Sites = sites
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new AdminDashboardViewModel());
            }
        }

        // Sites Management
        public async Task<IActionResult> Sites()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var sites = await _apiService.GetAsync<List<SiteDto>>("api/Admin/sites", token) ?? new List<SiteDto>();
            return View(sites);
        }

        [HttpGet]
        public IActionResult CreateSite()
        {
            return View(new CreateSiteDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSite(CreateSiteDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var result = await _apiService.PostAsync<object>("api/Admin/sites", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Şantiye başarıyla oluşturuldu";
                    return RedirectToAction("Sites");
                }

                TempData["ErrorMessage"] = "Şantiye oluşturulurken bir hata oluştu";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site");
                TempData["ErrorMessage"] = "Şantiye oluşturulurken bir hata oluştu";
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
                var site = await _apiService.GetAsync<SiteDto>($"api/Admin/sites/{id}", token);
                if (site == null)
                    return Json(new { success = false, message = "Şantiye bulunamadı" });

                // Basit HTML döndürme, partial view olmadan
                var html = $@"
                    <div class='row'>
                        <div class='col-md-6'>
                            <h6>Şantiye Bilgileri</h6>
                            <p><strong>Ad:</strong> {site.Name}</p>
                            <p><strong>Adres:</strong> {site.Address}</p>
                            <p><strong>Açıklama:</strong> {site.Description}</p>
                            <p><strong>Oluşturulma Tarihi:</strong> {site.CreatedDate:dd.MM.yyyy}</p>
                        </div>
                        <div class='col-md-6'>
                            <h6>Çalışan Bilgileri</h6>
                            <p><strong>Toplam Çalışan:</strong> {site.Employees?.Count ?? 0}</p>
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
                html.Append("<div class='table-responsive'>");
                html.Append("<table class='table table-sm'>");
                html.Append("<thead><tr><th>Ad Soyad</th><th>Pozisyon</th><th>E-posta</th><th>Durum</th></tr></thead>");
                html.Append("<tbody>");

                foreach (var emp in employees)
                {
                    var statusBadge = emp.IsActive ? 
                        "<span class='badge bg-success'>Aktif</span>" : 
                        "<span class='badge bg-danger'>Pasif</span>";
                    
                    html.Append($"<tr><td>{emp.FullName}</td><td>{emp.Position}</td><td>{emp.Email}</td><td>{statusBadge}</td></tr>");
                }

                html.Append("</tbody></table></div>");

                if (!employees.Any())
                {
                    html.Clear();
                    html.Append("<div class='text-center py-3'><p class='text-muted'>Bu şantiyede henüz çalışan bulunmuyor.</p></div>");
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
            {https://localhost:50276/Admin/Employees
                var payload = new { siteIds = siteIds, action = action };
                var result = await _apiService.PostAsync<object>("api/Admin/sites/bulk", payload, token);
                return Json(new { success = true, message = "İşlem başarıyla tamamlandı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk site action");
                return Json(new { success = false, message = "Toplu işlem sırasında hata oluştu" });
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

            var sites = await _apiService.GetAsync<List<SiteDto>>("api/Admin/sites", token) ?? new List<SiteDto>();
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
                var sites = await _apiService.GetAsync<List<SiteDto>>("api/Admin/sites", token) ?? new List<SiteDto>();
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
                var sitesForError = await _apiService.GetAsync<List<SiteDto>>("api/Admin/sites", token) ?? new List<SiteDto>();
                ViewBag.Sites = sitesForError;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                TempData["ErrorMessage"] = "Çalışan oluşturulurken bir hata oluştu";
                var sitesForError = await _apiService.GetAsync<List<SiteDto>>("api/Admin/sites", token) ?? new List<SiteDto>();
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