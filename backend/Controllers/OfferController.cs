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
    [Authorize]
    public class OfferController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public OfferController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> CreateOffer([FromBody] CreateOfferDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null)
                return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });

            if (supplier.Status != SupplierStatus.Approved)
                return BadRequest(new { message = "Onaylanmamış tedarikçi teklif veremez" });

            // Request kontrolü
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);

            if (request == null)
                return BadRequest(new { message = "Talep bulunamadı" });

            if (request.Status != RequestStatus.Open)
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
                Price = model.Price,
                Description = model.Description,
                DeliveryDays = model.DeliveryDays,
                Status = OfferStatus.Pending
            };

            _context.Offers.Add(offer);

            // Request durumunu güncelle
            request.Status = RequestStatus.InProgress;
            await _context.SaveChangesAsync();

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
                    if (offer.Request.Employee.UserId != userId)
                        return Forbid();
                    break;

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
                    if (request.Employee.UserId != userId)
                        return Forbid();
                    break;

                case "Supplier":
                    // Tedarikçiler sadece kendi tekliflerini görebilir
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.UserId == userId);
                    if (supplier == null)
                        return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });
                    
                    var supplierOffer = await _context.Offers
                        .Include(o => o.Supplier)
                            .ThenInclude(s => s.User)
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
                .Where(o => o.RequestId == requestId)
                .OrderBy(o => o.Price) // Fiyata göre sırala
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
