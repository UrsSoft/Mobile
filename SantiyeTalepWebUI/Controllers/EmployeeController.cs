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
                var myRequests = await _apiService.GetAsync<List<RequestDto>>("api/Request", token) ?? new List<RequestDto>();
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

            var requests = await _apiService.GetAsync<List<RequestDto>>("api/Request", token) ?? new List<RequestDto>();
            var model = new RequestListViewModel
            {
                Requests = requests
            };

            return View(model);
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
                    Title = model.Title,
                    Description = model.Description,
                    ProductDescription = model.ProductDescription,
                    Unit = model.Unit,
                    DeliveryType = model.DeliveryType,
                    Category = model.Category,
                    Quantity = model.Quantity,
                    RequiredDate = model.RequiredDate
                };

                var result = await _apiService.PostAsync<object>("api/Request", createDto, token);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Talep başarıyla oluşturuldu";
                    return RedirectToAction("Requests");
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
                var request = await _apiService.GetAsync<RequestDto>($"api/Request/{id}", token);
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

        public async Task<IActionResult> RequestOffers(int requestId)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var offers = await _apiService.GetAsync<List<OfferDto>>($"api/Offer/request/{requestId}", token) ?? new List<OfferDto>();
                var request = await _apiService.GetAsync<RequestDto>($"api/Request/{requestId}", token);

                ViewBag.Request = request;
                return View(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request offers");
                TempData["ErrorMessage"] = "Teklifler yüklenirken bir hata oluştu";
                return RedirectToAction("Requests");
            }
        }
    }
}