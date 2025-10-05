using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
using SantiyeTalepApi.Services;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider;

        public AdminController(ApplicationDbContext context, IMapper mapper, ILogger<AdminController> logger, INotificationService notificationService, IServiceProvider serviceProvider)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
            _serviceProvider = serviceProvider;
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
                return StatusCode(500, new { message = "Ýstatistikler yüklenirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Talepler yüklenirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Bekleyen tedarikçiler yüklenirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Tedarikçiler yüklenirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Bekleyen talepler yüklenirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Tamamlanan talepler yüklenirken hata oluþtu", error = ex.Message });
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

                _logger.LogInformation($"Validating requests: {string.Join(",", model.RequestIds)}");

                // Validate requests exist and are in appropriate status
                var requests = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.Site)
                    .Where(r => model.RequestIds.Contains(r.Id))
                    .ToListAsync();

                _logger.LogInformation($"Found {requests.Count} requests in database");

                if (requests.Count != model.RequestIds.Count)
                    return BadRequest(new { message = "Seçilen taleplerin bir kýsmý bulunamadý" });

                var invalidRequests = requests.Where(r => r.Status != RequestStatus.Open).ToList();
                if (invalidRequests.Any())
                {
                    var invalidRequestIds = string.Join(", ", invalidRequests.Select(r => r.Id));
                    return BadRequest(new { message = $"Sadece açýk durumundaki talepler tedarikçilere gönderilebilir. Geçersiz talepler: {invalidRequestIds}" });
                }

                _logger.LogInformation($"Validating suppliers: {string.Join(",", model.SupplierIds)}");

                // Validate suppliers exist and are approved
                var suppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => model.SupplierIds.Contains(s.Id) && s.Status == SupplierStatus.Approved)
                    .ToListAsync();

                _logger.LogInformation($"Found {suppliers.Count} approved suppliers in database");

                if (suppliers.Count != model.SupplierIds.Count)
                    return BadRequest(new { message = "Seçilen tedarikçilerin bir kýsmý bulunamadý veya onaylanmamýþ" });

                _logger.LogInformation("Updating request statuses to InProgress");

                // Update request status to InProgress
                foreach (var request in requests)
                {
                    request.Status = RequestStatus.InProgress;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Request statuses updated successfully");

                _logger.LogInformation("Starting to send notifications");

                // For better performance, we can use parallel execution with separate scopes
                // But for simplicity and reliability, we're using sequential execution
                // Send notifications to selected suppliers for each request sequentially to avoid DbContext threading issues
                foreach (var request in requests)
                {
                    foreach (var supplier in suppliers)
                    {
                        _logger.LogInformation($"Creating notification for supplier {supplier.Id} for request {request.Id}");
                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                "Yeni Teklif Talebi",
                                $"Yeni bir teklif talebi aldýnýz: {request.ProductDescription} (Miktar: {request.Quantity}). " +
                                $"Þantiye: {request.Site.Name}. Lütfen teklifinizi verin.",
                                NotificationType.RequestSentToSupplier,
                                supplier.UserId,
                                request.Id,
                                null,
                                supplier.Id
                            );
                            _logger.LogInformation($"Notification sent successfully to supplier {supplier.Id} for request {request.Id}");
                        }
                        catch (Exception notificationEx)
                        {
                            _logger.LogError(notificationEx, $"Failed to send notification to supplier {supplier.Id} for request {request.Id}");
                            // Continue with other notifications even if one fails
                        }
                    }
                }

                _logger.LogInformation("All notifications processed successfully");

                var result = new 
                { 
                    message = $"{requests.Count} talep {suppliers.Count} tedarikçiye baþarýyla gönderildi",
                    requestCount = requests.Count,
                    supplierCount = suppliers.Count,
                    affectedRequests = requests.Select(r => new { r.Id, r.ProductDescription }).ToList(),
                    notifiedSuppliers = suppliers.Select(s => new { s.Id, s.CompanyName }).ToList()
                };

                _logger.LogInformation($"Operation completed successfully: {result.message}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending requests to suppliers: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { message = "Talepler tedarikçilere gönderilirken bir hata oluþtu", error = ex.Message, details = ex.StackTrace });
            }
        }

        [HttpPost("requests/bulk")]
        public async Task<IActionResult> BulkRequestAction([FromBody] BulkRequestActionDto model)
        {
            try
            {
                if (model.RequestIds == null || !model.RequestIds.Any())
                    return BadRequest(new { message = "Talep seçimi yapýlmamýþ" });

                var requests = await _context.Requests
                    .Where(r => model.RequestIds.Contains(r.Id))
                    .ToListAsync();

                if (!requests.Any())
                    return NotFound(new { message = "Seçilen talepler bulunamadý" });

                switch (model.Action?.ToLower())
                {
                    case "approve":
                        requests.ForEach(r => r.Status = RequestStatus.Completed);
                        break;
                    case "inprogress":
                        requests.ForEach(r => r.Status = RequestStatus.InProgress);
                        break;
                    case "delete":
                        _context.Requests.RemoveRange(requests);
                        break;
                    default:
                        return BadRequest(new { message = "Geçersiz iþlem" });
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Toplu iþlem baþarýyla tamamlandý" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk request action");
                return StatusCode(500, new { message = "Toplu iþlem sýrasýnda hata oluþtu", error = ex.Message });
            }
        }

        [HttpPut("requests/{id}/status")]
        public async Task<IActionResult> ChangeRequestStatus(int id, [FromBody] ChangeRequestStatusDto model)
        {
            try
            {
                var request = await _context.Requests.FindAsync(id);
                if (request == null)
                    return NotFound(new { message = "Talep bulunamadý" });

                request.Status = (RequestStatus)model.Status;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Talep durumu güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing request status");
                return StatusCode(500, new { message = "Durum güncellenirken hata oluþtu", error = ex.Message });
            }
        }

        [HttpDelete("requests/{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var request = await _context.Requests.FindAsync(id);
                if (request == null)
                    return NotFound(new { message = "Talep bulunamadý" });

                _context.Requests.Remove(request);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Talep baþarýyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting request");
                return StatusCode(500, new { message = "Talep silinirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Þantiyeler yüklenirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Çalýþanlar yüklenirken hata oluþtu", error = ex.Message });
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
                return Ok(brandDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all brands");
                return StatusCode(500, new { message = "Markalar yüklenirken hata oluþtu", error = ex.Message });
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
                    return NotFound(new { message = "Teklif bulunamadý" });

                if (offer.Status != OfferStatus.Pending)
                    return BadRequest(new { message = "Sadece bekleyen teklifler onaylanabilir" });

                // Teklifi onayla
                offer.Status = OfferStatus.Approved;
                
                // Request'i tamamla
                offer.Request.Status = RequestStatus.Completed;

                // Ayný request için diðer pending teklifleri reddet
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
                        $"{offer.Request.ProductDescription} talebi için verdiðiniz teklif reddedildi. Baþka bir teklif seçilmiþtir.",
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
                    "Teklif Onaylandý",
                    $"Tebrikler! {offer.Request.ProductDescription} talebi için verdiðiniz {offer.FinalPrice:C} tutarýndaki teklif onaylandý.",
                    NotificationType.OfferApproved,
                    offer.Supplier.UserId,
                    offer.RequestId,
                    offer.Id,
                    offer.SupplierId
                );

                return Ok(new { message = "Teklif onaylandý ve diðer teklifler reddedildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving offer");
                return StatusCode(500, new { message = "Teklif onaylanýrken hata oluþtu", error = ex.Message });
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
                    return NotFound(new { message = "Teklif bulunamadý" });

                if (offer.Status != OfferStatus.Pending)
                    return BadRequest(new { message = "Sadece bekleyen teklifler reddedilebilir" });

                offer.Status = OfferStatus.Rejected;
                await _context.SaveChangesAsync();

                // Tedarikçiye red bildirimi gönder
                await _notificationService.CreateNotificationAsync(
                    "Teklif Reddedildi",
                    $"{offer.Request.ProductDescription} talebi için verdiðiniz teklif reddedildi.",
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
                return StatusCode(500, new { message = "Teklif reddedilirken hata oluþtu", error = ex.Message });
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
                return StatusCode(500, new { message = "Teklifler yüklenirken hata oluþtu", error = ex.Message });
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
                    .Where(o => o.Status == OfferStatus.Pending)
                    .OrderByDescending(o => o.OfferDate)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending offers");
                return StatusCode(500, new { message = "Bekleyen teklifler yüklenirken hata oluþtu", error = ex.Message });
            }
        }

        [HttpPut("suppliers/{id}/approve")]
        public async Task<IActionResult> ApproveSupplier(int id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = "Tedarikçi bulunamadý" });

                if (supplier.Status != SupplierStatus.Pending)
                    return BadRequest(new { message = "Sadece bekleyen tedarikçiler onaylanabilir" });

                supplier.Status = SupplierStatus.Approved;
                await _context.SaveChangesAsync();

                // Tedarikçiye onay bildirimi gönder
                await _notificationService.CreateNotificationAsync(
                    "Tedarikçi Baþvurunuz Onaylandý",
                    "Tebrikler! Tedarikçi baþvurunuz onaylandý. Artýk taleplere teklif verebilirsiniz.",
                    NotificationType.SupplierApproved,
                    supplier.UserId,
                    null,
                    null,
                    supplier.Id
                );

                return Ok(new { message = "Tedarikçi onaylandý" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving supplier");
                return StatusCode(500, new { message = "Tedarikçi onaylanýrken hata oluþtu", error = ex.Message });
            }
        }

        [HttpPut("suppliers/{id}/reject")]
        public async Task<IActionResult> RejectSupplier(int id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = "Tedarikçi bulunamadý" });

                if (supplier.Status != SupplierStatus.Pending)
                    return BadRequest(new { message = "Sadece bekleyen tedarikçiler reddedilebilir" });

                supplier.Status = SupplierStatus.Rejected;
                await _context.SaveChangesAsync();

                // Tedarikçiye red bildirimi gönder
                await _notificationService.CreateNotificationAsync(
                    "Tedarikçi Baþvurunuz Reddedildi",
                    "Maalesef tedarikçi baþvurunuz reddedildi. Daha fazla bilgi için admin ile iletiþime geçebilirsiniz.",
                    NotificationType.SupplierRejected,
                    supplier.UserId,
                    null,
                    null,
                    supplier.Id
                );

                return Ok(new { message = "Tedarikçi reddedildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting supplier");
                return StatusCode(500, new { message = "Tedarikçi reddedilirken hata oluþtu", error = ex.Message });
            }
        }
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

    public class ChangeRequestStatusDto
    {
        public int Status { get; set; }
    }
}