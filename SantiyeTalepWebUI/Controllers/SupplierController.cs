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
                var myOffers = await _apiService.GetAsync<List<OfferDto>>("api/Offer/my-offers", token) ?? new List<OfferDto>();
                // Bu endpoint artık tedarikçinin daha önce teklif verdiği talepleri filtreliyor
                var availableRequests = await _apiService.GetAsync<List<RequestDto>>("api/Request/open", token) ?? new List<RequestDto>();
                var myProfile = await _apiService.GetAsync<SupplierDto>("api/Supplier/profile", token);
                
                // Get notifications
                var notificationSummary = await _apiService.GetAsync<Models.DTOs.NotificationSummaryDto>("api/Notification/summary", token);
                var notifications = await _apiService.GetAsync<List<Models.DTOs.NotificationDto>>("api/Notification", token) ?? new List<Models.DTOs.NotificationDto>();

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
                    MyProfile = myProfile,
                    NotificationSummary = notificationSummary,
                    Notifications = notifications.Take(10).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier dashboard");
                return View(new SupplierDashboardViewModel());
            }
        }

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
                
                var response = await _apiService.PutAsync<dynamic>($"api/Notification/{id}/read", null, token);
                
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
                var response = await _apiService.PutAsync<dynamic>("api/Notification/mark-all-read", null, token);
                return Json(new { success = true, message = "Tüm bildirimler okundu olarak işaretlendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "Bildirimler güncellenirken hata oluştu" });
            }
        }

        public async Task<IActionResult> Offers()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                var offers = await _apiService.GetAsync<List<OfferDto>>("api/Offer/my-offers", token) ?? new List<OfferDto>();
                var model = new SantiyeTalepWebUI.Models.DTOs.OfferListViewModel
                {
                    Offers = offers
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading offers");
                var model = new SantiyeTalepWebUI.Models.DTOs.OfferListViewModel();
                return View(model);
            }
        }

        public async Task<IActionResult> AvailableRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                // Bu endpoint artık tedarikçinin daha önce teklif verdiği talepleri filtreliyor
                var requests = await _apiService.GetAsync<List<RequestDto>>("api/Request/open", token) ?? new List<RequestDto>();
                var model = new SantiyeTalepWebUI.Models.DTOs.RequestListViewModel
                {
                    Requests = requests
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available requests");
                var model = new SantiyeTalepWebUI.Models.DTOs.RequestListViewModel
                {
                    Requests = new List<RequestDto>()
                };
                return View(model);
            }
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
                    RequestId = requestId,
                    Quantity = request.Quantity, // Default to requested quantity
                    DeliveryDate = DateTime.Now.AddDays(14)
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
                // Calculate delivery days based on delivery type and date
                var deliveryDays = model.DeliveryType switch
                {
                    DeliveryType.TodayPickup => 0,
                    DeliveryType.SameDayDelivery => 0,
                    DeliveryType.NextDayDelivery => 1,
                    DeliveryType.BusinessDays1to2 => 2,
                    _ => (int)(model.DeliveryDate - DateTime.Now).TotalDays
                };

                // Create the API request model
                var apiModel = new
                {
                    RequestId = model.RequestId,
                    Brand = model.Brand,
                    Description = model.Description,
                    Quantity = model.Quantity,
                    Price = model.Price,
                    Currency = model.Currency,
                    Discount = model.Discount,
                    DeliveryType = model.DeliveryType,
                    DeliveryDays = deliveryDays
                };

                var result = await _apiService.PostAsync<OfferDto>("api/Offer", apiModel, token);
                if (result != null && result.Id > 0)
                {
                    TempData["SuccessMessage"] = "Teklif başarıyla oluşturuldu ve admin'e gönderildi";
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
                TempData["ErrorMessage"] = "Teklif oluşturulurken bir hata oluştu: " + ex.Message;
                try
                {
                    var requestForError = await _apiService.GetAsync<RequestDto>($"api/Request/{model.RequestId}", token);
                    ViewBag.Request = requestForError;
                }
                catch (Exception getEx)
                {
                    _logger.LogError(getEx, "Error loading request data after offer creation failure");
                }
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBulkOffers([FromBody] BulkCreateOfferDto model)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("CreateBulkOffers called without token");
                return Json(new { success = false, message = "Oturum süresi doldu" });
            }

            _logger.LogInformation("CreateBulkOffers called with {OfferCount} offers", model?.Offers?.Count ?? 0);

            try
            {
                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();

                foreach (var offer in model.Offers)
                {
                    _logger.LogDebug("Processing offer for request {RequestId}", offer.RequestId);
                    
                    if (string.IsNullOrWhiteSpace(offer.Brand) || 
                        string.IsNullOrWhiteSpace(offer.Description) || 
                        offer.Quantity <= 0 || 
                        offer.Price <= 0)
                    {
                        errorCount++;
                        errors.Add($"Talep #{offer.RequestId}: Eksik veya hatalı bilgi");
                        _logger.LogWarning("Validation failed for request {RequestId}: Brand={Brand}, Desc={Desc}, Qty={Qty}, Price={Price}", 
                            offer.RequestId, offer.Brand, offer.Description, offer.Quantity, offer.Price);
                        continue;
                    }

                    try
                    {
                        var deliveryDays = offer.DeliveryType switch
                        {
                            DeliveryType.TodayPickup => 0,
                            DeliveryType.SameDayDelivery => 0,
                            DeliveryType.NextDayDelivery => 1,
                            DeliveryType.BusinessDays1to2 => 2,
                            _ => 2
                        };

                        var apiModel = new
                        {
                            RequestId = offer.RequestId,
                            Brand = offer.Brand,
                            Description = offer.Description,
                            Quantity = offer.Quantity,
                            Price = offer.Price,
                            Currency = offer.Currency,
                            Discount = offer.Discount,
                            DeliveryType = offer.DeliveryType,
                            DeliveryDays = deliveryDays
                        };

                        _logger.LogDebug("Sending offer to API for request {RequestId} with brand {Brand}", offer.RequestId, offer.Brand);

                        var result = await _apiService.PostAsync<OfferDto>("api/Offer", apiModel, token);
                        
                        _logger.LogDebug("API response for request {RequestId}: {Result}", offer.RequestId, result != null ? "Success" : "Null");
                        
                        if (result != null && result.Id > 0)
                        {
                            successCount++;
                            _logger.LogInformation("Successfully created offer {OfferId} for request {RequestId}", result.Id, offer.RequestId);
                        }
                        else
                        {
                            errorCount++;
                            errors.Add($"Talep #{offer.RequestId}: Teklif oluşturulamadı - API null response");
                            _logger.LogWarning("Failed to create offer for request {RequestId} - API returned null", offer.RequestId);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"Talep #{offer.RequestId}: {ex.Message}");
                        _logger.LogError(ex, "Error creating individual offer for request {RequestId}", offer.RequestId);
                    }
                }

                var message = $"{successCount} teklif başarıyla oluşturuldu";
                if (errorCount > 0)
                {
                    message += $", {errorCount} teklif oluşturulamadı";
                }

                _logger.LogInformation("CreateBulkOffers completed: {SuccessCount} success, {ErrorCount} errors", successCount, errorCount);

                return Json(new 
                { 
                    success = successCount > 0, 
                    message = message,
                    successCount = successCount,
                    errorCount = errorCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk offers");
                return Json(new { success = false, message = "Toplu teklif oluşturulurken bir hata oluştu: " + ex.Message });
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawOffer(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                // Use dynamic type since withdraw endpoint may return different response structure
                var result = await _apiService.PutAsync<dynamic>($"api/Offer/{id}/withdraw", null, token);
                if (result != null)
                {
                    return Json(new { success = true, message = "Teklif başarıyla geri çekildi" });
                }

                return Json(new { success = false, message = "Teklif geri çekilirken bir hata oluştu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing offer");
                return Json(new { success = false, message = "Teklif geri çekilirken bir hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckNewRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var availableRequests = await _apiService.GetAsync<List<RequestDto>>("api/Request/open", token) ?? new List<RequestDto>();
                var notificationSummary = await _apiService.GetAsync<Models.DTOs.NotificationSummaryDto>("api/Notification/summary", token);
                
                return Json(new { 
                    success = true, 
                    data = new {
                        newRequestCount = availableRequests.Count,
                        unreadNotificationCount = notificationSummary?.UnreadCount ?? 0,
                        hasNewContent = availableRequests.Any() || (notificationSummary?.UnreadCount ?? 0) > 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking new requests");
                return Json(new { success = false, message = "Yeni talepler kontrol edilirken hata oluştu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestSiteBrands(int requestId)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                var brands = await _apiService.GetAsync<List<BrandDto>>($"api/Request/{requestId}/site-brands", token) ?? new List<BrandDto>();
                return Json(new { success = true, data = brands });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site brands for request {RequestId}", requestId);
                return Json(new { success = false, message = "Şantiye markaları yüklenirken hata oluştu" });
            }
        }
    }
}