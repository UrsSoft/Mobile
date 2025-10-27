using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
using System.Security.Claims;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Supplier")]
    public class SupplierController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(ApplicationDbContext context, IMapper mapper, ILogger<SupplierController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null)
                return BadRequest(new { message = "Tedarikçi bilgisi bulunamadý" });

            var supplierDto = _mapper.Map<SupplierDto>(supplier);
            return Ok(supplierDto);
        }

        [HttpGet("debug/notifications")]
        public async Task<IActionResult> GetDebugNotifications()
        {
            _logger.LogInformation("=== SUPPLIER DEBUG NOTIFICATIONS STARTED ===");
            
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                _logger.LogInformation("Debug notifications for user ID: {UserId}", userId);

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (supplier == null)
                {
                    _logger.LogError("Supplier not found for user ID: {UserId}", userId);
                    return BadRequest(new { message = "Tedarikçi bilgisi bulunamadý" });
                }

                _logger.LogInformation("Found supplier: {SupplierId} - {CompanyName} for user {UserId}", 
                    supplier.Id, supplier.CompanyName, userId);

                // Tüm bildirimleri getir
                var allNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId || n.SupplierId == supplier.Id)
                    .OrderByDescending(n => n.CreatedDate)
                    .Select(n => new {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.Type,
                        n.UserId,
                        n.RequestId,
                        n.OfferId,
                        n.SupplierId,
                        n.CreatedDate,
                        n.IsRead
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} total notifications for supplier", allNotifications.Count);

                // RequestSentToSupplier tipindeki notificationlarý özel olarak filtrele
                var requestNotifications = allNotifications
                    .Where(n => n.Type == NotificationType.RequestSentToSupplier)
                    .ToList();

                _logger.LogInformation("Found {Count} RequestSentToSupplier notifications", requestNotifications.Count);

                // Bu notificationlardaki RequestId'leri al
                var notifiedRequestIds = requestNotifications
                    .Where(n => n.RequestId.HasValue)
                    .Select(n => n.RequestId!.Value)
                    .Distinct()
                    .ToList();

                _logger.LogInformation("Notified request IDs: [{RequestIds}]", string.Join(", ", notifiedRequestIds));

                // Bu requestlerin durumlarýný kontrol et
                var requests = await _context.Requests
                    .Where(r => notifiedRequestIds.Contains(r.Id))
                    .Select(r => new {
                        r.Id,
                        r.ProductDescription,
                        r.Status,
                        r.RequestDate
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} requests:", requests.Count);
                foreach (var req in requests)
                {
                    _logger.LogInformation("Request {Id}: {Product} - Status: {Status} - Date: {Date}", 
                        req.Id, req.ProductDescription, req.Status, req.RequestDate);
                }

                // Tedarikçinin verdiði teklifleri kontrol et
                var offers = await _context.Offers
                    .Where(o => o.SupplierId == supplier.Id)
                    .Select(o => new {
                        o.Id,
                        o.RequestId,
                        o.Status,
                        o.OfferDate
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} offers from supplier:", offers.Count);
                foreach (var offer in offers)
                {
                    _logger.LogInformation("Offer {Id} for Request {RequestId} - Status: {Status} - Date: {Date}", 
                        offer.Id, offer.RequestId, offer.Status, offer.OfferDate);
                }

                var result = new
                {
                    UserId = userId,
                    SupplierId = supplier.Id,
                    CompanyName = supplier.CompanyName,
                    TotalNotifications = allNotifications.Count,
                    RequestNotifications = requestNotifications.Count,
                    NotifiedRequestIds = notifiedRequestIds,
                    Requests = requests,
                    Offers = offers,
                    AllNotifications = allNotifications
                };

                _logger.LogInformation("=== SUPPLIER DEBUG NOTIFICATIONS COMPLETED ===");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR IN SUPPLIER DEBUG NOTIFICATIONS ===");
                return StatusCode(500, new { 
                    message = "Debug bilgileri alýnýrken bir hata oluþtu", 
                    error = ex.Message 
                });
            }
        }
    }
}