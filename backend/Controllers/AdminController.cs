using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
using SantiyeTalepApi.Services;


namespace SantiyeTalepWebUI.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPushNotificationService _pushNotificationService;

        public AdminController(ApplicationDbContext context, IMapper mapper, ILogger<AdminController> logger, INotificationService notificationService, IServiceProvider serviceProvider, IPushNotificationService pushNotificationService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
            _serviceProvider = serviceProvider;
            _pushNotificationService = pushNotificationService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = new
                {
                    TotalRequests = await _context.Requests.CountAsync(),
                    PendingRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Open),
                    CompletedRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Completed),
                    ActiveSites = await _context.Sites.CountAsync(s => s.IsActive),
                    TotalEmployees = await _context.Employees.CountAsync(),
                    PendingSuppliers = await _context.Suppliers.CountAsync(s => s.Status == SupplierStatus.Pending),
                    TotalSuppliers = await _context.Suppliers.CountAsync(),
                    PendingOffers = await _context.Offers.CountAsync(o => o.Status == OfferStatus.Pending),
                    ApprovedOffers = await _context.Offers.CountAsync(o => o.Status == OfferStatus.Approved)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "İstatistikler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("site-performance")]
        public async Task<IActionResult> GetSitePerformanceData(string period = "1month")
        {
            try
            {
                // Calculate date range based on period
                DateTime startDate;
                var now = DateTime.Now;

                switch (period.ToLower())
                {
                    case "1month":
                        startDate = now.AddMonths(-1);
                        break;
                    case "3months":
                        startDate = now.AddMonths(-3);
                        break;
                    case "6months":
                        startDate = now.AddMonths(-6);
                        break;
                    case "12months":
                    case "1year":
                        startDate = now.AddMonths(-12);
                        break;
                    default:
                        startDate = now.AddMonths(-1);
                        break;
                }

                // Get all active sites with their request counts filtered by date
                var sites = await _context.Sites
                    .Where(s => s.IsActive)
                    .Include(s => s.Employees)
                        .ThenInclude(e => e.Requests.Where(r => r.RequestDate >= startDate))
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var sitePerformanceData = sites.Select(site => new
                {
                    siteName = site.Name,
                    totalRequests = site.Employees.SelectMany(e => e.Requests).Count(),
                    openRequests = site.Employees.SelectMany(e => e.Requests).Count(r => r.Status == RequestStatus.Open),
                    inProgressRequests = site.Employees.SelectMany(e => e.Requests).Count(r => r.Status == RequestStatus.InProgress),
                    completedRequests = site.Employees.SelectMany(e => e.Requests).Count(r => r.Status == RequestStatus.Completed),
                    cancelledRequests = site.Employees.SelectMany(e => e.Requests).Count(r => r.Status == RequestStatus.Cancelled),
                    employeeCount = site.Employees.Count
                }).ToList();

                return Ok(new { success = true, data = sitePerformanceData, period = period });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site performance data");
                return StatusCode(500, new { success = false, message = "Şantiye performans verileri yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetAllRequests()
        {
            try
            {
                var requests = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Site)
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                            .ThenInclude(s => s.User)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                var requestDtos = _mapper.Map<List<RequestDto>>(requests);
                return Ok(requestDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Talepler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("suppliers/approved")]
        public async Task<IActionResult> GetApprovedSuppliers()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.User)
                .Where(s => s.Status == SupplierStatus.Approved)
                .OrderBy(s => s.CompanyName)
                .ToListAsync();

            var supplierDtos = _mapper.Map<List<SupplierDto>>(suppliers);
            return Ok(supplierDtos);
        }

        [HttpGet("suppliers/pending")]
        public async Task<IActionResult> GetPendingSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => s.Status == SupplierStatus.Pending)
                    .OrderByDescending(s => s.Id)
                    .ToListAsync();

                var supplierDtos = _mapper.Map<List<SupplierDto>>(suppliers);
                return Ok(supplierDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending suppliers");
                return StatusCode(500, new { message = "Bekleyen tedarikçiler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetAllSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .OrderByDescending(s => s.Id)
                    .ToListAsync();

                var supplierDtos = _mapper.Map<List<SupplierDto>>(suppliers);
                return Ok(supplierDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all suppliers");
                return StatusCode(500, new { message = "Tedarikçiler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("suppliers/{id}")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = "Tedarikçi bulunamadı" });

                var supplierDto = _mapper.Map<SupplierDto>(supplier);
                return Ok(supplierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier by id: {SupplierId}", id);
                return StatusCode(500, new { message = "Tedarikçi yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("requests/pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var requests = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Site)
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                            .ThenInclude(s => s.User)
                    .Where(r => r.Status == RequestStatus.Open)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                var requestDtos = _mapper.Map<List<RequestDto>>(requests);
                return Ok(requestDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending requests");
                return StatusCode(500, new { message = "Bekleyen talepler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("requests/completed")]
        public async Task<IActionResult> GetCompletedRequests()
        {
            try
            {
                var requests = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Site)
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                            .ThenInclude(s => s.User)
                    .Where(r => r.Status == RequestStatus.Completed)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                var requestDtos = _mapper.Map<List<RequestDto>>(requests);
                return Ok(requestDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed requests");
                return StatusCode(500, new { message = "Tamamlanan talepler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("requests/send-to-suppliers")]
        public async Task<IActionResult> SendRequestsToSuppliers([FromBody] SendRequestsToSuppliersDto model)
        {
            try
            {
                _logger.LogInformation($"SendRequestsToSuppliers called with {model?.RequestIds?.Count ?? 0} requests and {model?.SupplierIds?.Count ?? 0} suppliers");

                if (model.RequestIds == null || !model.RequestIds.Any())
                    return BadRequest(new { message = "En az bir talep seçilmelidir" });

                if (model.SupplierIds == null || !model.SupplierIds.Any())
                    return BadRequest(new { message = "En az bir tedarikçi seçilmelidir" });

                _logger.LogInformation($"Request IDs: [{string.Join(",", model.RequestIds)}]");
                _logger.LogInformation($"Supplier IDs: [{string.Join(",", model.SupplierIds)}]");

                // Validate requests exist and are in appropriate status
                var requests = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.Site)
                    .Where(r => model.RequestIds.Contains(r.Id))
                    .ToListAsync();

                _logger.LogInformation($"Found {requests.Count} requests in database");

                if (requests.Count != model.RequestIds.Count)
                    return BadRequest(new { message = "Seçilen taleplerin bir kısmı bulunamadı" });

                var invalidRequests = requests.Where(r => r.Status != RequestStatus.Open).ToList();
                if (invalidRequests.Any())
                {
                    var invalidRequestIds = string.Join(", ", invalidRequests.Select(r => r.Id));
                    return BadRequest(new { message = $"Sadece açık durumundaki talepler tedarikçilere gönderilebilir. Geçersiz talepler: {invalidRequestIds}" });
                }

                _logger.LogInformation($"Validating suppliers: {string.Join(",", model.SupplierIds)}");

                // Validate suppliers exist and are approved
                var suppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => model.SupplierIds.Contains(s.Id) && s.Status == SupplierStatus.Approved)
                    .ToListAsync();

                _logger.LogInformation($"Found {suppliers.Count} approved suppliers in database");
                foreach (var supplier in suppliers)
                {
                    _logger.LogInformation($"Supplier: {supplier.Id} - {supplier.CompanyName} (User: {supplier.UserId})");
                }

                if (suppliers.Count != model.SupplierIds.Count)
                    return BadRequest(new { message = "Seçilen tedarikçilerin bir kısmı bulunamadı veya onaylanmamış" });

                _logger.LogInformation("Updating request statuses to InProgress");

                // Update request status to InProgress
                foreach (var request in requests)
                {
                    request.Status = RequestStatus.InProgress;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Request statuses updated successfully");

                _logger.LogInformation("Starting to send notifications");

                // Send notifications to selected suppliers for each request sequentially to avoid DbContext threading issues
                foreach (var request in requests)
                {
                    foreach (var supplier in suppliers)
                    {
                        _logger.LogInformation($"Creating notification for supplier {supplier.Id} (User: {supplier.UserId}) for request {request.Id}");
                        try
                        {
                            var notificationTitle = "Yeni Teklif Talebi";
                            var notificationMessage = $"Yeni bir teklif talebi aldınız: {request.ProductDescription} (Miktar: {request.Quantity}). " +
                                $"Şantiye: {request.Site.Name}. Lütfen teklifinizi verin.";

                            _logger.LogInformation($"Notification details - Title: {notificationTitle}, Message: {notificationMessage}");

                            await _notificationService.CreateNotificationAsync(
                                notificationTitle,
                                notificationMessage,
                                NotificationType.RequestSentToSupplier,
                                supplier.UserId,
                                request.Id,
                                null,
                                supplier.Id
                            );
                            _logger.LogInformation($"Notification sent successfully to supplier {supplier.Id} (User: {supplier.UserId}) for request {request.Id}");

                            // Notification'ın gerçekten oluştuğunu kontrol edelim
                            var createdNotification = await _context.Notifications
                                .Where(n => n.Type == NotificationType.RequestSentToSupplier
                                           && n.UserId == supplier.UserId
                                           && n.RequestId == request.Id
                                           && n.SupplierId == supplier.Id)
                                .FirstOrDefaultAsync();

                            if (createdNotification != null)
                            {
                                _logger.LogInformation($"Verified notification creation - ID: {createdNotification.Id}, Created: {createdNotification.CreatedDate}");
                            }
                            else
                            {
                                _logger.LogError($"Failed to verify notification creation for supplier {supplier.Id} and request {request.Id}");
                            }
                        }
                        catch (Exception notificationEx)
                        {
                            _logger.LogError(notificationEx, $"Failed to send notification to supplier {supplier.Id} for request {request.Id}");
                            // Continue with other notifications even if one fails
                        }
                    }
                }

                // Send push notifications to suppliers
                _logger.LogInformation("Starting to send push notifications");
                try
                {
                    var supplierIds = suppliers.Select(s => s.Id).ToList();
                    var requestTitles = string.Join(", ", requests.Select(r => r.ProductDescription).Take(3));

                    if (requests.Count > 3)
                    {
                        requestTitles += $" ve {requests.Count - 3} tane daha";
                    }

                    await _pushNotificationService.SendNotificationToSuppliersAsync(
                        supplierIds,
                        "Yeni Teklif Talebi",
                        $"Size {requests.Count} adet yeni teklif talebi gönderildi: {requestTitles}",
                        new { type = "new_request", requestCount = requests.Count }
                    );

                    _logger.LogInformation($"Push notifications sent to {supplierIds.Count} suppliers");
                }
                catch (Exception pushEx)
                {
                    _logger.LogError(pushEx, "Failed to send push notifications");
                    // Continue even if push notifications fail
                }

                _logger.LogInformation("All notifications processed successfully");

                var result = new
                {
                    message = $"{requests.Count} talep {suppliers.Count} tedarikçiye başarıyla gönderildi",
                    requestCount = requests.Count,
                    supplierCount = suppliers.Count,
                    affectedRequests = requests.Select(r => new { r.Id, r.ProductDescription }).ToList(),
                    notifiedSuppliers = suppliers.Select(s => new { s.Id, s.CompanyName, s.UserId }).ToList()
                };

                _logger.LogInformation($"Operation completed successfully: {result.message}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending requests to suppliers: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { message = "Talepler tedarikçilere gönderilirken bir hata oluştu", error = ex.Message, details = ex.StackTrace });
            }
        }

        [HttpPost("requests/bulk")]
        public async Task<IActionResult> BulkRequestAction([FromBody] BulkRequestActionDto model)
        {
            try
            {
                if (model.RequestIds == null || !model.RequestIds.Any())
                    return BadRequest(new { message = "Talep seçimi yapılmamış" });

                var requests = await _context.Requests
                    .Where(r => model.RequestIds.Contains(r.Id))
                    .ToListAsync();

                if (!requests.Any())
                    return NotFound(new { message = "Seçilen talepler bulunamadı" });

                switch (model.Action?.ToLower())
                {
                    case "open":
                        requests.ForEach(r => r.Status = RequestStatus.Open);
                        break;
                    case "approve":
                        requests.ForEach(r => r.Status = RequestStatus.Completed);
                        break;
                    case "inprogress":
                        requests.ForEach(r => r.Status = RequestStatus.InProgress);
                        break;
                    case "delete":
                        var notifications = _context.Notifications.Where(n => model.RequestIds.Contains(n.Id)).ToList();
                        _context.Notifications.RemoveRange(notifications);
                        _context.Requests.RemoveRange(requests);
                        break;
                    default:
                        return BadRequest(new { message = "Geçersiz işlem" });
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Toplu işlem başarıyla tamamlandı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk request action");
                return StatusCode(500, new { message = "Toplu işlem sırasında hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("requests/{id}/status")]
        public async Task<IActionResult> ChangeRequestStatus(int id, [FromBody] ChangeRequestStatusDto model)
        {
            try
            {
                var request = await _context.Requests.FindAsync(id);
                if (request == null)
                    return NotFound(new { message = "Talep bulunamadı" });

                request.Status = (RequestStatus)model.Status;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Talep durumu güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing request status");
                return StatusCode(500, new { message = "Durum güncellenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpDelete("requests/{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var request = await _context.Requests.FindAsync(id);
                if (request == null)
                    return NotFound(new { message = "Talep bulunamadı" });
                var notification = _context.Notifications.Where(n => n.RequestId == id).FirstOrDefault();
                _context.Notifications.Remove(notification);
                _context.Requests.Remove(request);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Talep başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting request");
                return StatusCode(500, new { message = "Talep silinirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("sites")]
        public async Task<IActionResult> GetAllSites()
        {
            try
            {
                var sites = await _context.Sites
                    .Include(s => s.SiteBrands)
                        .ThenInclude(sb => sb.Brand)
                    .Include(s => s.Employees)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var siteDtos = _mapper.Map<List<SiteDto>>(sites);
                return Ok(siteDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sites");
                return StatusCode(500, new { message = "Şantiyeler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("sites/{id}")]
        public async Task<IActionResult> GetSiteById(int id)
        {
            try
            {
                var site = await _context.Sites
                    .Include(s => s.SiteBrands)
                        .ThenInclude(sb => sb.Brand)
                    .Include(s => s.Employees)
                        .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (site == null)
                    return NotFound(new { message = "Şantiye bulunamadı" });

                var siteDto = _mapper.Map<SiteDto>(site);
                return Ok(siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site by id: {SiteId}", id);
                return StatusCode(500, new { message = "Şantiye yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("sites/{id}/employees")]
        public async Task<IActionResult> GetSiteEmployees(int id)
        {
            try
            {
                var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == id);
                if (site == null)
                    return NotFound(new { message = "Şantiye bulunamadı" });

                var employees = await _context.Employees
                    .Include(e => e.User)
                    .Where(e => e.SiteId == id)
                    .OrderBy(e => e.User.FullName)
                    .ToListAsync();

                var employeeDtos = _mapper.Map<List<EmployeeDto>>(employees);
                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site employees: {SiteId}", id);
                return StatusCode(500, new { message = "Şantiye çalışanları yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("sites")]
        public async Task<IActionResult> CreateSite([FromBody] CreateSiteDto model)
        {
            try
            {
                // ❌ YANLIŞ - String interpolation
                // _logger.LogInformation($"CreateSite called with model: {System.Text.Json.JsonSerializer.Serialize(model)}");

                // ✅ DOĞRU - Structured logging
                _logger.LogInformation("CreateSite called with model: {Model}", System.Text.Json.JsonSerializer.Serialize(model));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>());
                    // ❌ _logger.LogWarning($"CreateSite ModelState invalid: {string.Join(", ", errors)}");
                    // ✅ DOĞRU
                    _logger.LogWarning("CreateSite ModelState invalid: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { message = "Geçersiz veri", errors = errors });
                }

                var brandIds = model.BrandIds?.ToList() ?? new List<int>();

                // Sorguyu çalıştır
                // 1. Önce tüm aktif markaları çek
                var allActiveBrands = await _context.Brands
                    .Where(b => b.IsActive)
                    .ToListAsync();

                // 2. Memory'de filtrele (Contains() SQL'e çevrilmez)
                var brands = allActiveBrands
                    .Where(b => brandIds.Contains(b.Id))
                    .ToList();

                // ❌ _logger.LogInformation($"Found {brands.Count} brands out of {model.BrandIds.Count} requested");
                // ✅ DOĞRU
                _logger.LogInformation("Found {BrandsCount} brands out of {RequestedCount} requested", brands.Count, model.BrandIds.Count);

                if (brands.Count != model.BrandIds.Count)
                {
                    var foundBrandIds = brands.Select(b => b.Id).ToList();
                    var missingBrandIds = model.BrandIds.Except(foundBrandIds).ToList();
                    // ❌ _logger.LogWarning($"Some brands not found or inactive: {string.Join(", ", missingBrandIds)}");
                    // ✅ DOĞRU
                    _logger.LogWarning("Some brands not found or inactive: {MissingBrandIds}", string.Join(", ", missingBrandIds));
                    return BadRequest(new { message = "Seçilen markalardan bir kısmı bulunamadı veya aktif değil" });
                }

                // Create site
                var site = new Site
                {
                    Name = model.Name.Trim(),
                    Address = model.Address.Trim(),
                    Description = model.Description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Sites.Add(site);
                await _context.SaveChangesAsync();
                // ❌ _logger.LogInformation($"Site created with ID: {site.Id}");
                // ✅ DOĞRU
                _logger.LogInformation("Site created with ID: {SiteId}", site.Id);

                // Create site-brand relationships
                foreach (var brandId in model.BrandIds)
                {
                    var siteBrand = new SiteBrand
                    {
                        SiteId = site.Id,
                        BrandId = brandId
                    };
                    _context.SiteBrands.Add(siteBrand);
                }

                await _context.SaveChangesAsync();
                // ❌ _logger.LogInformation($"Site-brand relationships created for site {site.Id}");
                // ✅ DOĞRU
                _logger.LogInformation("Site-brand relationships created for site {SiteId}", site.Id);

                // Load created site with brands for response
                var createdSite = await _context.Sites
                    .Include(s => s.SiteBrands)
                        .ThenInclude(sb => sb.Brand)
                    .Include(s => s.Employees)
                        .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(s => s.Id == site.Id);

                var siteDto = _mapper.Map<SiteDto>(createdSite);
                // ❌ _logger.LogInformation($"Site creation completed successfully: {siteDto.Id}");
                // ✅ DOĞRU
                _logger.LogInformation("Site creation completed successfully: {SiteId}", siteDto.Id);

                return Ok(siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site: {Message}", ex.Message);
                return StatusCode(500, new { message = "Şantiye oluşturulurken hata oluştu", error = ex.Message });
            }
        }


        [HttpPut("sites/{id}")]
        public async Task<IActionResult> UpdateSite(int id, [FromBody] UpdateSiteDto model)
        {
            try
            {
                _logger.LogInformation($"UpdateSite called for ID: {id} with model: {System.Text.Json.JsonSerializer.Serialize(model)}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(new { message = "Geçersiz veri", errors = errors });
                }

                var site = await _context.Sites
                    .Include(s => s.SiteBrands)
                    .Include(s => s.Employees)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (site == null)
                    return NotFound(new { message = "Şantiye bulunamadı" });

                var brandIds = model.BrandIds?.ToList() ?? new List<int>();
                // Validate brands exist
                // Sorguyu çalıştır
                // 1. Önce tüm aktif markaları çek
                var allActiveBrands = await _context.Brands
                    .Where(b => b.IsActive)
                    .ToListAsync();

                // 2. Memory'de filtrele (Contains() SQL'e çevrilmez)
                var brands = allActiveBrands
                    .Where(b => brandIds.Contains(b.Id))
                    .ToList();

                if (brands.Count != model.BrandIds.Count)
                    return BadRequest(new { message = "Seçilen markalardan bir kısmı bulunamadı veya aktif değil" });

                // Update site properties
                site.Name = model.Name.Trim();
                site.Address = model.Address.Trim();
                site.Description = model.Description?.Trim();
                site.IsActive = model.IsActive;

                // Remove existing site-brand relationships
                _context.SiteBrands.RemoveRange(site.SiteBrands);

                // Add new site-brand relationships
                foreach (var brandId in model.BrandIds)
                {
                    var siteBrand = new SiteBrand
                    {
                        SiteId = site.Id,
                        BrandId = brandId
                    };
                    _context.SiteBrands.Add(siteBrand);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Site updated successfully: {site.Id}");

                // Load updated site with brands and employees for response
                var updatedSite = await _context.Sites
                    .Include(s => s.SiteBrands)
                        .ThenInclude(sb => sb.Brand)
                    .Include(s => s.Employees)
                        .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                var siteDto = _mapper.Map<SiteDto>(updatedSite);
                return Ok(siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site {SiteId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Şantiye güncellenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpDelete("sites/{id}")]
        public async Task<IActionResult> DeleteSite(int id)
        {
            try
            {
                var site = await _context.Sites
                    .Include(s => s.Employees)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (site == null)
                    return NotFound(new { message = "Şantiye bulunamadı" });

                // Check if site has employees
                if (site.Employees?.Any() == true)
                {
                    return BadRequest(new
                    {
                        message = "Bu şantiyede çalışan bulunduğu için silinemez. Önce çalışanları başka şantiyelere transfer edin veya hesaplarını silin.",
                        employeeCount = site.Employees.Count
                    });
                }

                // Remove site-brand relationships first
                var siteBrands = await _context.SiteBrands.Where(sb => sb.SiteId == id).ToListAsync();
                _context.SiteBrands.RemoveRange(siteBrands);

                // Remove the site
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Site deleted successfully: {id}");
                return Ok(new { message = "Şantiye başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site {SiteId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Şantiye silinirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("sites/bulk")]
        public async Task<IActionResult> BulkSiteAction([FromBody] BulkSiteActionDto model)
        {
            try
            {
                if (model.SiteIds == null || !model.SiteIds.Any())
                    return BadRequest(new { message = "Şantiye seçimi yapılmamış" });

                var sites = await _context.Sites
                    .Include(s => s.Employees)
                    .Where(s => model.SiteIds.Contains(s.Id))
                    .ToListAsync();

                if (!sites.Any())
                    return NotFound(new { message = "Seçilen şantiyeler bulunamadı" });

                switch (model.Action?.ToLower())
                {
                    case "activate":
                        sites.ForEach(s => s.IsActive = true);
                        break;
                    case "deactivate":
                        sites.ForEach(s => s.IsActive = false);
                        break;
                    case "delete":
                        // Check if any site has employees
                        var sitesWithEmployees = sites.Where(s => s.Employees?.Any() == true).ToList();
                        if (sitesWithEmployees.Any())
                        {
                            var siteNames = string.Join(", ", sitesWithEmployees.Select(s => s.Name));
                            return BadRequest(new
                            {
                                message = $"Aşağıdaki şantiyelerde çalışan bulunduğu için silme işlemi tamamlanamadı: {siteNames}. Önce tüm çalışanları transfer edin veya silin."
                            });
                        }

                        // Remove site-brand relationships first
                        var siteBrands = await _context.SiteBrands.Where(sb => model.SiteIds.Contains(sb.SiteId)).ToListAsync();
                        _context.SiteBrands.RemoveRange(siteBrands);

                        // Remove sites
                        _context.Sites.RemoveRange(sites);
                        break;
                    default:
                        return BadRequest(new { message = "Geçersiz işlem" });
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Toplu işlem başarıyla tamamlandı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk site action");
                return StatusCode(500, new { message = "Toplu işlem sırasında hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var employees = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Site)
                    .OrderByDescending(e => e.Id)
                    .ToListAsync();

                var employeeDtos = _mapper.Map<List<EmployeeDto>>(employees);
                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all employees");
                return StatusCode(500, new { message = "Çalışanlar yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("employees/{id}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Site)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    return NotFound(new { message = "Çalışan bulunamadı" });

                var employeeDto = _mapper.Map<EmployeeDto>(employee);
                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee by id: {EmployeeId}", id);
                return StatusCode(500, new { message = "Çalışan yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("employees/available")]
        public async Task<IActionResult> GetAvailableEmployees()
        {
            try
            {
                // Get employees who are not assigned to any site or have inactive sites
                var availableEmployees = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Site)
                    .Where(e => e.User.IsActive &&
                               (e.Site == null || !e.Site.IsActive))
                    .ToListAsync();

                var employeeDtos = _mapper.Map<List<EmployeeDto>>(availableEmployees);
                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available employees");
                return StatusCode(500, new { message = "Kullanılabilir çalışanlar alınırken hata oluştu" });
            }
        }

        [HttpGet("brands")]
        public async Task<IActionResult> GetAllBrands()
        {
            try
            {
                var brands = await _context.Brands
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                var brandDtos = _mapper.Map<List<BrandDto>>(brands);
                _logger.LogInformation($"Found {brands.Count} active brands");
                return Ok(brandDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all brands");
                return StatusCode(500, new { message = "Markalar yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("employees")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                    return BadRequest(new { message = "Bu e-posta adresi zaten kullanılıyor" });

                // Check if phone already exists
                var existingPhone = await _context.Users.FirstOrDefaultAsync(u => u.Phone == model.Phone);
                if (existingPhone != null)
                    return BadRequest(new { message = "Bu telefon numarası zaten kullanılıyor" });

                // Check if site exists
                var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == model.SiteId);
                if (site == null)
                    return BadRequest(new { message = "Seçilen şantiye bulunamadı" });

                // Create user
                var user = new User
                {
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Role = UserRole.Employee,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create employee
                var employee = new Employee
                {
                    UserId = user.Id,
                    SiteId = model.SiteId,
                    Position = model.Position,
                    CreatedDate = DateTime.Now
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Load employee with related data for mapping
                var createdEmployee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Site)
                    .FirstOrDefaultAsync(e => e.Id == employee.Id);

                var employeeDto = _mapper.Map<EmployeeDto>(createdEmployee);
                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                return StatusCode(500, new { message = "Çalışan oluşturulurken hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("employees/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto model)
        {
            try
            {
                _logger.LogInformation($"UpdateEmployee called for ID: {id} with model: {System.Text.Json.JsonSerializer.Serialize(model)}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(new { message = "Geçersiz veri", errors = errors });
                }

                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Site)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    return NotFound(new { message = "Çalışan bulunamadı" });

                // Check if site exists
                var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == model.SiteId);
                if (site == null)
                    return BadRequest(new { message = "Seçilen şantiye bulunamadı" });

                // Check if phone is already used by another user
                var existingPhone = await _context.Users
                    .Where(u => u.Phone == model.Phone && u.Id != employee.UserId)
                    .FirstOrDefaultAsync();
                if (existingPhone != null)
                    return BadRequest(new { message = "Bu telefon numarası başka bir kullanıcı tarafından kullanılıyor" });

                // Update user properties
                employee.User.FullName = model.FullName.Trim();
                employee.User.Phone = model.Phone.Trim();
                employee.User.IsActive = model.IsActive;

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    employee.User.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    _logger.LogInformation($"Password updated for employee user: {employee.UserId}");
                }

                // Update employee properties
                employee.Position = model.Position.Trim();
                employee.SiteId = model.SiteId;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Employee updated successfully: {employee.Id}");

                // Load updated employee with related data for response
                var updatedEmployee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Site)
                    .FirstOrDefaultAsync(e => e.Id == id);

                var employeeDto = _mapper.Map<EmployeeDto>(updatedEmployee);
                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Çalışan güncellenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpDelete("employees/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                _logger.LogInformation($"DeleteEmployee called for ID: {id}");

                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    _logger.LogWarning($"Employee not found with ID: {id}");
                    return NotFound(new { message = "Çalışan bulunamadı" });
                }

                // Check if employee has active requests
                var hasActiveRequests = await _context.Requests
                    .AnyAsync(r => r.EmployeeId == id && r.Status != RequestStatus.Completed && r.Status != RequestStatus.Cancelled);

                if (hasActiveRequests)
                {
                    _logger.LogWarning($"Cannot delete employee {id} - has active requests");
                    return BadRequest(new { message = "Bu çalışanın aktif talepleri bulunduğu için silinemez. Önce tüm taleplerini tamamlayın veya iptal edin." });
                }

                // Delete employee first
                _context.Employees.Remove(employee);

                // Then delete associated user
                _context.Users.Remove(employee.User);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Employee and associated user deleted successfully: {id}");

                return Ok(new { message = "Çalışan başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Çalışan silinirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("employees/{id}/status")]
        public async Task<IActionResult> ToggleEmployeeStatus(int id, [FromBody] ToggleEmployeeStatusDto model)
        {
            try
            {
                _logger.LogInformation($"ToggleEmployeeStatus called for ID: {id} with isActive: {model.IsActive}");

                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    _logger.LogWarning($"Employee not found with ID: {id}");
                    return NotFound(new { message = "Çalışan bulunamadı" });
                }

                // Update user status
                employee.User.IsActive = model.IsActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Employee status updated successfully: {id}, IsActive: {model.IsActive}");
                return Ok(new { message = $"Çalışan durumu {(model.IsActive ? "aktif" : "pasif")} olarak güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling employee status {EmployeeId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Çalışan durumu güncellenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("suppliers")]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto model)
        {
            try
            {
                _logger.LogInformation($"CreateSupplier called with model: {System.Text.Json.JsonSerializer.Serialize(model)}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"CreateSupplier ModelState invalid: {string.Join(", ", errors)}");
                    return BadRequest(new { message = "Geçersiz veri", errors = errors });
                }

                // Check if email already exists
                var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingEmail != null)
                {
                    _logger.LogWarning($"Email already exists: {model.Email}");
                    return BadRequest(new { message = "Bu e-posta adresi zaten kullanılıyor" });
                }

                // Check if phone already exists
                var existingPhone = await _context.Users.FirstOrDefaultAsync(u => u.Phone == model.Phone);
                if (existingPhone != null)
                {
                    _logger.LogWarning($"Phone already exists: {model.Phone}");
                    return BadRequest(new { message = "Bu telefon numarası zaten kullanılıyor" });
                }

                // Check if tax number already exists
                var existingTaxNumber = await _context.Suppliers.FirstOrDefaultAsync(s => s.TaxNumber == model.TaxNumber);
                if (existingTaxNumber != null)
                {
                    _logger.LogWarning($"Tax number already exists: {model.TaxNumber}");
                    return BadRequest(new { message = "Bu vergi numarası zaten kayıtlı" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create user
                    var user = new User
                    {
                        Email = model.Email.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        FullName = model.FullName.Trim(),
                        Phone = model.Phone.Trim(),
                        Role = UserRole.Supplier,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"User created with ID: {user.Id}");

                    // Create supplier with Approved status (admin-created suppliers are auto-approved)
                    var supplier = new Supplier
                    {
                        UserId = user.Id,
                        CompanyName = model.CompanyName.Trim(),
                        TaxNumber = model.TaxNumber.Trim(),
                        Address = model.Address.Trim(),
                        Status = SupplierStatus.Approved,
                        ApprovedDate = DateTime.Now,
                        ApprovalNote = "Admin tarafından oluşturuldu - otomatik onaylı",
                        CreatedDate = DateTime.Now
                    };

                    _context.Suppliers.Add(supplier);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Supplier created with ID: {supplier.Id}");

                    await transaction.CommitAsync();

                    // Load created supplier with user data for response
                    var createdSupplier = await _context.Suppliers
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == supplier.Id);

                    var supplierDto = _mapper.Map<SupplierDto>(createdSupplier);
                    _logger.LogInformation($"Supplier creation completed successfully: {supplierDto.Id}");

                    return Ok(supplierDto);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed during supplier creation");
                    return StatusCode(500, new { message = "Tedarikçi kaydı sırasında bir hata oluştu", error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier: {Message}", ex.Message);
                return StatusCode(500, new { message = "Tedarikçi oluşturulurken bir hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("suppliers/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(new { message = "Geçersiz veri", errors = errors });
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = "Tedarikçi bulunamadı" });

                // Check if phone is already used by another user
                var existingPhone = await _context.Users
                    .Where(u => u.Phone == model.Phone && u.Id != supplier.UserId)
                    .FirstOrDefaultAsync();
                if (existingPhone != null)
                    return BadRequest(new { message = "Bu telefon numarası başka bir kullanıcı tarafından kullanılıyor" });

                // Check if email is already used by another user (only if email changed)
                if (!string.IsNullOrEmpty(model.Email) && model.Email.Trim() != supplier.User.Email)
                {
                    var existingEmail = await _context.Users
                        .Where(u => u.Email == model.Email && u.Id != supplier.UserId)
                        .FirstOrDefaultAsync();
                    if (existingEmail != null)
                        return BadRequest(new { message = "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor" });
                }

                // Check if tax number is already used by another supplier (only if changed)
                if (!string.IsNullOrEmpty(model.TaxNumber) && model.TaxNumber.Trim() != supplier.TaxNumber)
                {
                    var existingTaxNumber = await _context.Suppliers
                        .Where(s => s.TaxNumber == model.TaxNumber && s.Id != id)
                        .FirstOrDefaultAsync();
                    if (existingTaxNumber != null)
                    {
                        _logger.LogWarning($"Tax number already exists: {model.TaxNumber}");
                        return BadRequest(new { message = "Bu vergi numarası başka bir tedarikçi tarafından kullanılıyor" });
                    }
                }

                // Update user properties
                supplier.User.FullName = model.FullName.Trim();
                supplier.User.Email = model.Email.Trim();
                supplier.User.Phone = model.Phone.Trim();
                supplier.User.IsActive = model.IsActive;

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    supplier.User.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    _logger.LogInformation($"Password updated for supplier user: {supplier.UserId}");
                }

                // Update supplier properties
                supplier.CompanyName = model.CompanyName.Trim();
                supplier.TaxNumber = model.TaxNumber.Trim();
                supplier.Address = model.Address.Trim();

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Supplier updated successfully: {supplier.Id}");

                var supplierDto = _mapper.Map<SupplierDto>(supplier);
                return Ok(supplierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Tedarikçi güncellenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpDelete("suppliers/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .Include(s => s.Offers)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = "Tedarikçi bulunamadı" });

                // Check if supplier has any offers
                if (supplier.Offers?.Any() == true)
                {
                    return BadRequest(new
                    {
                        message = "Bu tedarikçinin mevcut teklifleri bulunduğu için silinemez.",
                        offerCount = supplier.Offers.Count
                    });
                }

                // Delete supplier first
                _context.Suppliers.Remove(supplier);

                // Then delete associated user
                _context.Users.Remove(supplier.User);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Supplier and associated user deleted successfully: {id}");

                return Ok(new { message = "Tedarikçi başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier {SupplierId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Tedarikçi silinirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("offers")]
        public async Task<IActionResult> GetAllOffers()
        {
            try
            {
                var offers = await _context.Offers
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.User)
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .OrderByDescending(o => o.OfferDate)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all offers");
                return StatusCode(500, new { message = "Teklifler yüklenirken hata oluştu", error = ex.Message });
            }
        }
        [HttpGet("offers/pending")]
        public async Task<IActionResult> GetPendingOffers()
        {
            try
            {
                var offers = await _context.Offers
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.User)
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .OrderByDescending(o => o.OfferDate).Where(o => o.Status == OfferStatus.Pending)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all pending offers");
                return StatusCode(500, new { message = "Teklifler yüklenirken hata oluştu", error = ex.Message });
            }
        }
        [HttpGet("offers/approved")]
        public async Task<IActionResult> GetApprovedOffers()
        {
            try
            {
                var offers = await _context.Offers
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.User)
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .OrderByDescending(o => o.OfferDate).Where(o => o.Status == OfferStatus.Approved)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all approved offers");
                return StatusCode(500, new { message = "Teklifler yüklenirken hata oluştu", error = ex.Message });
            }
        }
        [HttpGet("offers/rejected")]
        public async Task<IActionResult> GetRejectedOffers()
        {
            try
            {
                var offers = await _context.Offers
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.User)
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .OrderByDescending(o => o.OfferDate).Where(o => o.Status == OfferStatus.Rejected)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all rejected offers");
                return StatusCode(500, new { message = "Teklifler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("offers/{id}/approve")]
        public async Task<IActionResult> ApproveOffer(int id)
        {
            try
            {
                var offer = await _context.Offers
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.User)
                    .Include(o => o.Request)
                        .ThenInclude(r => r.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (offer == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                if (offer.Status != OfferStatus.Pending)
                    return BadRequest(new { message = "Sadece bekleyen teklifler onaylanabilir" });

                // Teklifi onayla
                offer.Status = OfferStatus.Approved;

                // Request'i tamamla
                offer.Request.Status = RequestStatus.Completed;

                // Aynı request için diğer pending teklifleri reddet
                var otherOffers = await _context.Offers
                    .Where(o => o.RequestId == offer.RequestId && o.Id != offer.Id && o.Status == OfferStatus.Pending)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .ToListAsync();

                foreach (var otherOffer in otherOffers)
                {
                    otherOffer.Status = OfferStatus.Rejected;

                    // Reddedilen tedarikçilere bildirim gönder
                    await _notificationService.CreateNotificationAsync(
                        "Teklif Reddedildi",
                        $"{offer.Request.ProductDescription} talebi için verdiğiniz teklif reddedildi. Başka bir teklif seçilmiştir.",
                        NotificationType.OfferRejected,
                        otherOffer.Supplier.UserId,
                        offer.RequestId,
                        otherOffer.Id,
                        otherOffer.SupplierId
                    );
                }

                await _context.SaveChangesAsync();

                // Onaylanan tedarikçiye bildirim gönder
                await _notificationService.CreateNotificationAsync(
                    "Teklif Onaylandı",
                    $"Tebrikler! {offer.Request.ProductDescription} talebi için verdiğiniz {offer.FinalPrice:C} tutarındaki teklif onaylandı.",
                    NotificationType.OfferApproved,
                    offer.Supplier.UserId,
                    offer.RequestId,
                    offer.Id,
                    offer.SupplierId
                );

                return Ok(new { message = "Teklif onaylandı ve diğer teklifler reddedildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving offer");
                return StatusCode(500, new { message = "Teklif onaylanırken hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("offers/{id}/reject")]
        public async Task<IActionResult> RejectOffer(int id)
        {
            try
            {
                var offer = await _context.Offers
                    .Include(o => o.Request)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (offer == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                if (offer.Status != OfferStatus.Pending)
                    return BadRequest(new { message = "Sadece bekleyen teklifler reddedilebilir" });

                offer.Status = OfferStatus.Rejected;
                await _context.SaveChangesAsync();

                // Tedarikçiye red bildirimi gönder
                await _notificationService.CreateNotificationAsync(
                    "Teklif Reddedildi",
                    $"{offer.Request.ProductDescription} talebi için verdiğiniz teklif reddedildi.",
                    NotificationType.OfferRejected,
                    offer.Supplier.UserId,
                    offer.RequestId,
                    offer.Id,
                    offer.SupplierId
                );

                return Ok(new { message = "Teklif reddedildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting offer");
                return StatusCode(500, new { message = "Teklif reddedilirken hata oluştu", error = ex.Message });
            }
        }

        // Note: ApproveSupplier and RejectSupplier endpoints removed 
        // because only Admin can create suppliers and they are auto-approved.
        // Suppliers cannot self-register anymore.
    }

    // DTO classes for request operations
    public class SendRequestsToSuppliersDto
    {
        public List<int> RequestIds { get; set; } = new();
        public List<int> SupplierIds { get; set; } = new();
    }

    public class BulkRequestActionDto
    {
        public List<int> RequestIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
    }

    public class BulkOfferActionDto
    {
        public List<int> OfferIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
    }

    public class ChangeRequestStatusDto
    {
        public int Status { get; set; }
    }

    // DTO classes for new operations
    public class BulkSiteActionDto
    {
        public List<int> SiteIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
    }

    public class ToggleEmployeeStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class ToggleSupplierStatusDto
    {
        public bool IsActive { get; set; }
    }
}