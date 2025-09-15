using Microsoft.AspNetCore.Mvc;
using SantiyeTalepWebUI.Models;
using SantiyeTalepWebUI.Models.DTOs;
using SantiyeTalepWebUI.Models.ViewModels;
using SantiyeTalepWebUI.Services;
using SantiyeTalepWebUI.Filters;

namespace SantiyeTalepWebUI.Controllers
{
    [AuthorizeRole(UserRole.Supplier)]
    public class SupplierController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(IApiService apiService, IAuthService authService, ILogger<SupplierController> logger)
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
                var myOffers = await _apiService.GetAsync<List<OfferDto>>("api/Offer", token) ?? new List<OfferDto>();
                var availableRequests = await _apiService.GetAsync<List<RequestDto>>("api/Request/open", token) ?? new List<RequestDto>();
                var myProfile = await _apiService.GetAsync<SupplierDto>("api/Supplier/profile", token);

                var stats = new DashboardStats
                {
                    MyOffers = myOffers.Count,
                    PendingOffers = myOffers.Count(o => o.Status == OfferStatus.Pending),
                    ApprovedOffers = myOffers.Count(o => o.Status == OfferStatus.Approved)
                };

                var model = new SupplierDashboardViewModel
                {
                    Stats = stats,
                    MyOffers = myOffers.Take(10).ToList(),
                    AvailableRequests = availableRequests.Take(10).ToList(),
                    MyProfile = myProfile
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier dashboard");
                return View(new SupplierDashboardViewModel());
            }
        }

        public async Task<IActionResult> Offers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var offers = await _apiService.GetAsync<List<OfferDto>>("api/Offer", token) ?? new List<OfferDto>();
            var model = new OfferListViewModel
            {
                Offers = offers
            };

            return View(model);
        }

        public async Task<IActionResult> AvailableRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var requests = await _apiService.GetAsync<List<RequestDto>>("api/Request/open", token) ?? new List<RequestDto>();
            var model = new RequestListViewModel
            {
                Requests = requests
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOffer(int requestId)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var request = await _apiService.GetAsync<RequestDto>($"api/Request/{requestId}", token);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Talep bulunamadı";
                    return RedirectToAction("AvailableRequests");
                }

                ViewBag.Request = request;
                var model = new CreateOfferDto
                {
                    RequestId = requestId
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request for offer");
                TempData["ErrorMessage"] = "Talep bilgileri yüklenirken bir hata oluştu";
                return RedirectToAction("AvailableRequests");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOffer(CreateOfferDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                try
                {
                    var request = await _apiService.GetAsync<RequestDto>($"api/Request/{model.RequestId}", token);
                    ViewBag.Request = request;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading request for offer validation");
                }
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<object>("api/Offer", model, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Teklif başarıyla oluşturuldu";
                    return RedirectToAction("Offers");
                }

                TempData["ErrorMessage"] = "Teklif oluşturulurken bir hata oluştu";
                var requestForError = await _apiService.GetAsync<RequestDto>($"api/Request/{model.RequestId}", token);
                ViewBag.Request = requestForError;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating offer");
                TempData["ErrorMessage"] = "Teklif oluşturulurken bir hata oluştu";
                var requestForError = await _apiService.GetAsync<RequestDto>($"api/Request/{model.RequestId}", token);
                ViewBag.Request = requestForError;
                return View(model);
            }
        }

        public async Task<IActionResult> OfferDetails(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var offer = await _apiService.GetAsync<OfferDto>($"api/Offer/{id}", token);
                if (offer == null)
                {
                    TempData["ErrorMessage"] = "Teklif bulunamadı";
                    return RedirectToAction("Offers");
                }

                return View(offer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading offer details");
                TempData["ErrorMessage"] = "Teklif detayları yüklenirken bir hata oluştu";
                return RedirectToAction("Offers");
            }
        }

        public async Task<IActionResult> RequestDetails(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var request = await _apiService.GetAsync<RequestDto>($"api/Request/{id}", token);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Talep bulunamadı";
                    return RedirectToAction("AvailableRequests");
                }

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request details");
                TempData["ErrorMessage"] = "Talep detayları yüklenirken bir hata oluştu";
                return RedirectToAction("AvailableRequests");
            }
        }
    }
}