using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
using SantiyeTalepApi.Services;
using System.Security.Claims;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OfferController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public OfferController(ApplicationDbContext context, IMapper mapper, INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> CreateOffer([FromBody] CreateOfferDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null)
                return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });

            if (supplier.Status != SupplierStatus.Approved)
                return BadRequest(new { message = "Onaylanmamış tedarikçi teklif veremez" });

            // Tedarikçinin bu talebe erişim yetkisi var mı kontrol et
            var hasAccess = await _context.Notifications
                .AnyAsync(n => n.Type == NotificationType.RequestSentToSupplier 
                              && n.UserId == userId 
                              && n.RequestId == model.RequestId);

            if (!hasAccess)
                return Forbid("Bu talebe teklif verme yetkiniz bulunmamaktadır");

            // Request kontrolü
            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.User)
                .Include(r => r.Site)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);

            if (request == null)
                return BadRequest(new { message = "Talep bulunamadı" });

            if (request.Status != RequestStatus.Open && request.Status != RequestStatus.InProgress)
                return BadRequest(new { message = "Bu talep için teklif verilemez" });

            // Daha önce teklif verilip verilmediğini kontrol et
            var existingOffer = await _context.Offers
                .FirstOrDefaultAsync(o => o.RequestId == model.RequestId && o.SupplierId == supplier.Id);

            if (existingOffer != null)
                return BadRequest(new { message = "Bu talep için zaten teklif verilmiş" });

            var offer = new Offer
            {
                RequestId = model.RequestId,
                SupplierId = supplier.Id,
                Brand = model.Brand,
                Description = model.Description,
                Quantity = model.Quantity,
                Price = model.Price,
                Currency = model.Currency,
                Discount = model.Discount,
                DeliveryType = model.DeliveryType,
                DeliveryDays = model.DeliveryDays,
                Status = OfferStatus.Pending
            };

            _context.Offers.Add(offer);

            // Request durumunu güncelle
            request.Status = RequestStatus.InProgress;
            await _context.SaveChangesAsync();

            // Admin'e bildirim gönder
            await _notificationService.CreateAdminNotificationAsync(
                "Yeni Teklif Alındı",
                $"{supplier.CompanyName} ({supplier.User.FullName}) {request.ProductDescription} talebi için {offer.FinalPrice:C} tutarında teklif verdi.",
                NotificationType.NewOffer,
                request.Id,
                offer.Id,
                supplier.Id
            );

            // Offer'ı detaylarıyla birlikte geri döndür
            var createdOffer = await _context.Offers
                .Include(o => o.Request)
                    .ThenInclude(r => r.Employee)
                        .ThenInclude(e => e.User)
                .Include(o => o.Request)
                    .ThenInclude(r => r.Site)
                .Include(o => o.Supplier)
                    .ThenInclude(s => s.User)
                .FirstAsync(o => o.Id == offer.Id);

            var offerDto = _mapper.Map<OfferDto>(createdOffer);
            return CreatedAtAction(nameof(GetOffer), new { id = offer.Id }, offerDto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOffer(int id)
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
                return NotFound();

            // Access control
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            switch (userRole)
            {
                case "Supplier":
                    if (offer.Supplier.UserId != userId)
                        return Forbid();
                    break;

                case "Employee":
                    // Employees are now restricted from viewing offer details for security
                    return Forbid("Çalışanlar teklif detaylarını görüntüleyemez. Bu bilgiler güvenlik amacıyla sadece yönetim tarafından görüntülenebilir.");

                case "Admin":
                    // Admin tüm teklifleri görebilir
                    break;

                default:
                    return Forbid();
            }

            var offerDto = _mapper.Map<OfferDto>(offer);
            return Ok(offerDto);
        }

        [HttpGet("request/{requestId}")]
        public async Task<IActionResult> GetOffersByRequest(int requestId)
        {
            var request = await _context.Requests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return NotFound(new { message = "Talep bulunamadı" });

            // Access control
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            switch (userRole)
            {
                case "Employee":
                    // Employees are now restricted from viewing offer details for security
                    return Forbid("Çalışanlar teklif detaylarını görüntüleyemez. Bu bilgiler güvenlik amacıyla sadece yönetim tarafından görüntülenebilir.");

                case "Supplier":
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.UserId == userId);
                    if (supplier == null)
                        return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });
                    
                    // Tedarikçinin bu talebe erişim yetkisi var mı kontrol et
                    var hasAccess = await _context.Notifications
                        .AnyAsync(n => n.Type == NotificationType.RequestSentToSupplier 
                                      && n.UserId == userId 
                                      && n.RequestId == requestId);

                    if (!hasAccess)
                        return Forbid("Bu talebe erişim yetkiniz bulunmamaktadır");
                    
                    var supplierOffer = await _context.Offers
                        .Include(o => o.Supplier)
                            .ThenInclude(s => s.User)
                        .Include(o => o.Request)
                        .FirstOrDefaultAsync(o => o.RequestId == requestId && o.SupplierId == supplier.Id);
                    
                    if (supplierOffer == null)
                        return NotFound(new { message = "Bu talep için teklifiniz bulunamadı" });
                    
                    var supplierOfferDto = _mapper.Map<OfferDto>(supplierOffer);
                    return Ok(new List<OfferDto> { supplierOfferDto });

                case "Admin":
                    // Admin tüm teklifleri görebilir
                    break;

                default:
                    return Forbid();
            }

            var offers = await _context.Offers
                .Include(o => o.Supplier)
                    .ThenInclude(s => s.User)
                .Include(o => o.Request)
                .Where(o => o.RequestId == requestId)
                .OrderBy(o => o.FinalPrice) // Final price'a göre sırala
                .ToListAsync();

            var offerDtos = _mapper.Map<List<OfferDto>>(offers);
            return Ok(offerDtos);
        }

        [HttpGet("my-offers")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> GetMyOffers()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null)
                return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });

            var offers = await _context.Offers
                .Include(o => o.Request)
                    .ThenInclude(r => r.Employee)
                        .ThenInclude(e => e.User)
                .Include(o => o.Request)
                    .ThenInclude(r => r.Site)
                .Include(o => o.Supplier)
                    .ThenInclude(s => s.User)
                .Where(o => o.SupplierId == supplier.Id)
                .OrderByDescending(o => o.OfferDate)
                .ToListAsync();

            var offerDtos = _mapper.Map<List<OfferDto>>(offers);
            return Ok(offerDtos);
        }

        [HttpPut("{id}/withdraw")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> WithdrawOffer(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null)
                return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });

            var offer = await _context.Offers
                .Include(o => o.Request)
                .FirstOrDefaultAsync(o => o.Id == id && o.SupplierId == supplier.Id);

            if (offer == null)
                return NotFound(new { message = "Teklif bulunamadı" });

            if (offer.Status != OfferStatus.Pending)
                return BadRequest(new { message = "Sadece bekleyen teklifler geri çekilebilir" });

            offer.Status = OfferStatus.Rejected;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Teklif geri çekildi" });
        }
    }
}
