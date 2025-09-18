using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AdminController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
                    .Where(r => r.Status == RequestStatus.Open)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                var requestDtos = _mapper.Map<List<RequestDto>>(requests);
                return Ok(requestDtos);
            }
            catch (Exception ex)
            {
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
                    .Where(r => r.Status == RequestStatus.Completed)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                var requestDtos = _mapper.Map<List<RequestDto>>(requests);
                return Ok(requestDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Tamamlanan talepler yüklenirken hata oluştu", error = ex.Message });
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
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
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
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .Where(o => o.Status == OfferStatus.Pending)
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bekleyen teklifler yüklenirken hata oluştu", error = ex.Message });
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
                        .ThenInclude(r => r.Employee)
                            .ThenInclude(e => e.Site)
                    .Include(o => o.Supplier)
                        .ThenInclude(s => s.User)
                    .Where(o => o.Status == OfferStatus.Approved)
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();

                var offerDtos = _mapper.Map<List<OfferDto>>(offers);
                return Ok(offerDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Onaylı teklifler yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("sites/{id}/employees")]
        public async Task<IActionResult> GetSiteEmployees(int id)
        {
            try
            {
                var employees = await _context.Employees
                    .Include(e => e.User)
                    .Where(e => e.SiteId == id)
                    .ToListAsync();

                var employeeDtos = _mapper.Map<List<EmployeeDto>>(employees);
                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şantiye çalışanları yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpDelete("sites/{id}")]
        public async Task<IActionResult> DeleteSite(int id)
        {
            try
            {
                var site = await _context.Sites
                    .Include(s => s.SiteBrands)
                    .FirstOrDefaultAsync(s => s.Id == id);
                    
                if (site == null)
                    return NotFound(new { message = "Şantiye bulunamadı" });

                // Check if site has employees
                var hasEmployees = await _context.Employees.AnyAsync(e => e.SiteId == id);
                if (hasEmployees)
                    return BadRequest(new { message = "Bu şantiyede çalışan bulunduğu için silinemez" });

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Remove site-brand relationships first
                    if (site.SiteBrands?.Any() == true)
                    {
                        _context.SiteBrands.RemoveRange(site.SiteBrands);
                    }

                    // Remove the site
                    _context.Sites.Remove(site);
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "Şantiye ve atanmış markalar başarıyla silindi" });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
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
                        var siteIds = sites.Select(s => s.Id).ToList();
                        var hasEmployees = await _context.Employees.AnyAsync(e => siteIds.Contains(e.SiteId));
                        if (hasEmployees)
                            return BadRequest(new { message = "Çalışan bulunan şantiyeler silinemez" });
                        
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
                return StatusCode(500, new { message = "Toplu işlem sırasında hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("employees/{id}/status")]
        public async Task<IActionResult> ToggleEmployeeStatus(int id, [FromBody] ToggleEmployeeStatusDto model)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    return NotFound(new { message = "Çalışan bulunamadı" });

                employee.User.IsActive = model.IsActive;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Çalışan durumu başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Durum güncellenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpDelete("employees/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    return NotFound(new { message = "Çalışan bulunamadı" });

                // Check if employee has active requests
                var hasActiveRequests = await _context.Requests
                    .AnyAsync(r => r.EmployeeId == id && r.Status == RequestStatus.Open);

                if (hasActiveRequests)
                    return BadRequest(new { message = "Bu çalışanın aktif talepleri bulunduğu için silinemez" });

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Employees.Remove(employee);
                    _context.Users.Remove(employee.User);
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "Çalışan başarıyla silindi" });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Çalışan silinirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("employees")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Email kontrolü
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest(new { message = "Bu email adresi zaten kullanılıyor" });

            // Site kontrolü
            var site = await _context.Sites.FindAsync(model.SiteId);
            if (site == null || !site.IsActive)
                return BadRequest(new { message = "Geçersiz şantiye" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // User oluştur
                var user = new User
                {
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Role = UserRole.Employee,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Employee oluştur
                var employee = new Employee
                {
                    UserId = user.Id,
                    SiteId = model.SiteId,
                    Position = model.Position
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Employee'yi geri döndür
                var createdEmployee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Site)
                    .FirstAsync(e => e.Id == employee.Id);

                var employeeDto = _mapper.Map<EmployeeDto>(createdEmployee);
                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employeeDto);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Çalışan oluşturulurken bir hata oluştu" });
            }
        }

        [HttpGet("employees/{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Site)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return Ok(employeeDto);
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Site)
                .ToListAsync();

            var employeeDtos = _mapper.Map<List<EmployeeDto>>(employees);
            return Ok(employeeDtos);
        }

        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
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
                return StatusCode(500, new { message = "Markalar yüklenirken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("sites")]
        public async Task<IActionResult> CreateSite([FromBody] CreateSiteDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate brands exist and are active
                if (model.BrandIds?.Any() == true)
                {
                    var validBrandIds = await _context.Brands
                        .Where(b => b.IsActive && model.BrandIds.Contains(b.Id))
                        .Select(b => b.Id)
                        .ToListAsync();

                    if (validBrandIds.Count != model.BrandIds.Count)
                    {
                        return BadRequest(new { message = "Seçilen markaların bir kısmı geçersiz" });
                    }
                }
                else
                {
                    return BadRequest(new { message = "En az bir marka seçilmelidir" });
                }

                // Create site
                var site = new Site
                {
                    Name = model.Name,
                    Address = model.Address,
                    Description = model.Description,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                // Add site-brand relationships
                var siteBrands = model.BrandIds.Select(brandId => new SiteBrand
                {
                    SiteId = site.Id,
                    BrandId = brandId
                }).ToList();

                _context.SiteBrands.AddRange(siteBrands);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Return the created site with brands
                var createdSite = await _context.Sites
                    .Include(s => s.SiteBrands)
                        .ThenInclude(sb => sb.Brand)
                    .FirstAsync(s => s.Id == site.Id);

                var siteDto = _mapper.Map<SiteDto>(createdSite);
                return CreatedAtAction(nameof(GetSite), new { id = site.Id }, siteDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Şantiye oluşturularken bir hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("sites/{id}")]
        public async Task<IActionResult> GetSite(int id)
        {
            var site = await _context.Sites
                .Include(s => s.SiteBrands)
                    .ThenInclude(sb => sb.Brand)
                .Include(s=> s.Employees)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (site == null)
                return NotFound();

            var siteDto = _mapper.Map<SiteDto>(site);
            return Ok(siteDto);
        }

        [HttpGet("sites")]
        public async Task<IActionResult> GetSites()
        {
            var sites = await _context.Sites
                .Include(s => s.SiteBrands)
                    .ThenInclude(sb => sb.Brand)
                .Include(s=> s.Employees)
                .ToListAsync();
                
            var siteDtos = _mapper.Map<List<SiteDto>>(sites);
            return Ok(siteDtos);
        }

        [HttpPut("sites/{id}")]
        public async Task<IActionResult> UpdateSite(int id, [FromBody] UpdateSiteDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != model.Id)
                return BadRequest(new { message = "ID uyumsuzluğu" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var site = await _context.Sites
                    .Include(s => s.SiteBrands)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (site == null)
                    return NotFound(new { message = "Şantiye bulunamadı" });

                // Validate brands exist and are active if provided
                if (model.BrandIds?.Any() == true)
                {
                    var validBrandIds = await _context.Brands
                        .Where(b => b.IsActive && model.BrandIds.Contains(b.Id))
                        .Select(b => b.Id)
                        .ToListAsync();

                    if (validBrandIds.Count != model.BrandIds.Count)
                    {
                        return BadRequest(new { message = "Seçilen markaların bir kısmı geçersiz" });
                    }
                }

                // Update site properties
                site.Name = model.Name;
                site.Address = model.Address;
                site.Description = model.Description;
                site.IsActive = model.IsActive;

                // Update brand relationships
                if (model.BrandIds?.Any() == true)
                {
                    // Remove existing brand relationships
                    _context.SiteBrands.RemoveRange(site.SiteBrands);

                    // Add new brand relationships
                    var newSiteBrands = model.BrandIds.Select(brandId => new SiteBrand
                    {
                        SiteId = site.Id,
                        BrandId = brandId
                    }).ToList();

                    _context.SiteBrands.AddRange(newSiteBrands);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return updated site
                var updatedSite = await _context.Sites
                    .Include(s => s.SiteBrands)
                        .ThenInclude(sb => sb.Brand)
                    .FirstAsync(s => s.Id == id);

                var siteDto = _mapper.Map<SiteDto>(updatedSite);
                return Ok(siteDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Şantiye güncellenirken bir hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("suppliers/{id}/approve")]
        public async Task<IActionResult> ApproveSupplier(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
                return NotFound(new { message = "Tedarikçi bulunamadı" });

            supplier.Status = SupplierStatus.Approved;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tedarikçi onaylandı" });
        }

        [HttpPut("suppliers/{id}/reject")]
        public async Task<IActionResult> RejectSupplier(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
                return NotFound(new { message = "Tedarikçi bulunamadı" });

            supplier.Status = SupplierStatus.Rejected;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tedarikçi reddedildi" });
        }

        [HttpGet("suppliers/pending")]
        public async Task<IActionResult> GetPendingSuppliers()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.User)
                .Where(s => s.Status == SupplierStatus.Pending)
                .ToListAsync();

            var supplierDtos = _mapper.Map<List<SupplierDto>>(suppliers);
            return Ok(supplierDtos);
        }

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetAllSuppliers()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.User)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var supplierDtos = _mapper.Map<List<SupplierDto>>(suppliers);
            return Ok(supplierDtos);
        }

        [HttpGet("suppliers/approved")]
        public async Task<IActionResult> GetApprovedSuppliers()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.User)
                .Where(s => s.Status == SupplierStatus.Approved)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var supplierDtos = _mapper.Map<List<SupplierDto>>(suppliers);
            return Ok(supplierDtos);
        }

        [HttpPut("offers/{id}/approve")]
        public async Task<IActionResult> ApproveOffer(int id)
        {
            var offer = await _context.Offers
                .Include(o => o.Request)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
                return NotFound(new { message = "Teklif bulunamadı" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Teklifi onayla
                offer.Status = OfferStatus.Approved;
                
                // Diğer teklifleri reddet
                var otherOffers = await _context.Offers
                    .Where(o => o.RequestId == offer.RequestId && o.Id != offer.Id)
                    .ToListAsync();

                foreach (var otherOffer in otherOffers)
                {
                    otherOffer.Status = OfferStatus.Rejected;
                }

                // Talebi tamamlandı olarak işaretle
                offer.Request.Status = RequestStatus.Completed;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Teklif onaylandı ve talep tamamlandı" });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Teklif onaylanırken bir hata oluştu" });
            }
        }

        [HttpPut("offers/{id}/reject")]
        public async Task<IActionResult> RejectOffer(int id)
        {
            try
            {
                var offer = await _context.Offers.FindAsync(id);
                if (offer == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                offer.Status = OfferStatus.Rejected;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Teklif reddedildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Teklif reddedilirken hata oluştu", error = ex.Message });
            }
        }
    }

    // DTO classes for new endpoints
    public class BulkSiteActionDto
    {
        public List<int> SiteIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
    }

    public class ToggleEmployeeStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class UpdateSiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<int> BrandIds { get; set; } = new();
    }
}
