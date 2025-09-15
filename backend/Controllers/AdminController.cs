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

        [HttpPost("sites")]
        public async Task<IActionResult> CreateSite([FromBody] CreateSiteDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var site = _mapper.Map<Site>(model);
            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            var siteDto = _mapper.Map<SiteDto>(site);
            return CreatedAtAction(nameof(GetSite), new { id = site.Id }, siteDto);
        }

        [HttpGet("sites/{id}")]
        public async Task<IActionResult> GetSite(int id)
        {
            var site = await _context.Sites.FindAsync(id);
            if (site == null)
                return NotFound();

            var siteDto = _mapper.Map<SiteDto>(site);
            return Ok(siteDto);
        }

        [HttpGet("sites")]
        public async Task<IActionResult> GetSites()
        {
            var sites = await _context.Sites.ToListAsync();
            var siteDtos = _mapper.Map<List<SiteDto>>(sites);
            return Ok(siteDtos);
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
    }
}
