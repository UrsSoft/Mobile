using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
using SantiyeTalepApi.Services;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExcelRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ExcelRequestController> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;

        public ExcelRequestController(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<ExcelRequestController> logger,
            IFileStorageService fileStorageService,
            INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Admin: Excel talep oluþturma
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ExcelRequestDto>> CreateExcelRequest([FromForm] CreateExcelRequestDto dto, [FromForm] IFormFile excelFile)
        {
            try
            {
                _logger.LogInformation("Creating Excel request for Site {SiteId} by Employee {EmployeeId}", dto.SiteId, dto.EmployeeId);

                // Validate file
                if (excelFile == null || excelFile.Length == 0)
                {
                    return BadRequest(new { message = "Excel dosyasý seçilmedi" });
                }

                // Validate site and employee
                var site = await _context.Sites.FindAsync(dto.SiteId);
                if (site == null)
                {
                    return NotFound(new { message = "Þantiye bulunamadý" });
                }

                var employee = await _context.Employees.FindAsync(dto.EmployeeId);
                if (employee == null)
                {
                    return NotFound(new { message = "Çalýþan bulunamadý" });
                }

                // Validate suppliers
                var suppliers = await _context.Suppliers
                    .Where(s => dto.SupplierIds.Contains(s.Id) && s.Status == SupplierStatus.Approved)
                    .ToListAsync();

                if (suppliers.Count != dto.SupplierIds.Count)
                {
                    return BadRequest(new { message = "Bazý tedarikçiler bulunamadý veya onaylý deðil" });
                }

                // Save Excel file
                var (storedFileName, filePath, fileSize) = await _fileStorageService
                    .SaveExcelFileAsync(excelFile, "AdminUploads");

                // Create Excel request
                var excelRequest = new ExcelRequest
                {
                    SiteId = dto.SiteId,
                    EmployeeId = dto.EmployeeId,
                    OriginalFileName = excelFile.FileName,
                    StoredFileName = storedFileName,
                    FilePath = filePath,
                    FileSize = fileSize,
                    Status = ExcelRequestStatus.AssignedToSuppliers,
                    Description = dto.Description,
                    UploadedDate = DateTime.Now
                };

                _context.ExcelRequests.Add(excelRequest);
                await _context.SaveChangesAsync();

                // Assign to suppliers
                foreach (var supplierId in dto.SupplierIds)
                {
                    var assignment = new ExcelRequestSupplier
                    {
                        ExcelRequestId = excelRequest.Id,
                        SupplierId = supplierId,
                        AssignedDate = DateTime.Now
                    };
                    _context.ExcelRequestSuppliers.Add(assignment);
                }

                await _context.SaveChangesAsync();

                // Send notifications to suppliers
                await SendNotificationsToSuppliers(excelRequest.Id, suppliers, site.Name);

                _logger.LogInformation("Excel request {Id} created successfully", excelRequest.Id);

                return Ok(new { success = true, message = "Excel dosyasý baþarýyla yüklendi ve tedarikçilere gönderildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Excel request");
                return StatusCode(500, new { success = false, message = "Excel yüklenirken hata oluþtu: " + ex.Message });
            }
        }

        /// <summary>
        /// Tedarikçi: Atanan Excel taleplerini listele
        /// </summary>
        [HttpGet("supplier/assigned")]
        [Authorize(Roles = "Supplier")]
        public async Task<ActionResult<List<SupplierExcelRequestDto>>> GetAssignedExcelRequests()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var user = await _context.Users
                    .Include(u => u.Supplier)
                    .FirstOrDefaultAsync(u => u.FullName == userEmail);

                if (user?.Supplier == null)
                {
                    return NotFound(new { message = "Tedarikçi bulunamadý" });
                }

                var assignments = await _context.ExcelRequestSuppliers
                    .Include(ers => ers.ExcelRequest)
                        .ThenInclude(er => er.Site)
                    .Include(ers => ers.ExcelRequest)
                        .ThenInclude(er => er.Employee)
                            .ThenInclude(e => e.User)
                    .Where(ers => ers.SupplierId == user.Supplier.Id)
                    .OrderByDescending(ers => ers.AssignedDate)
                    .ToListAsync();

                var result = assignments.Select(a => new SupplierExcelRequestDto
                {
                    Id = a.Id,
                    ExcelRequestId = a.ExcelRequestId,
                    SiteName = a.ExcelRequest.Site?.Name ?? "",
                    EmployeeName = a.ExcelRequest.Employee?.User?.FullName ?? "",
                    OriginalFileName = a.ExcelRequest.OriginalFileName,
                    FileSize = a.ExcelRequest.FileSize,
                    AssignedDate = a.AssignedDate,
                    Downloaded = a.Downloaded,
                    DownloadedDate = a.DownloadedDate,
                    OfferUploaded = a.OfferUploaded,
                    OfferUploadedDate = a.OfferUploadedDate,
                    Description = a.ExcelRequest.Description ?? ""
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned Excel requests");
                return StatusCode(500, new { message = "Excel talepleri yüklenirken hata oluþtu" });
            }
        }

        /// <summary>
        /// Tedarikçi: Excel dosyasýný indir
        /// </summary>
        [HttpGet("supplier/download/{assignmentId}")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> DownloadExcelFile(int assignmentId)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var user = await _context.Users
                    .Include(u => u.Supplier)
                    .FirstOrDefaultAsync(u => u.FullName == userEmail);

                if (user?.Supplier == null)
                {
                    return NotFound(new { message = "Tedarikçi bulunamadý" });
                }

                var assignment = await _context.ExcelRequestSuppliers
                    .Include(ers => ers.ExcelRequest)
                    .FirstOrDefaultAsync(ers => ers.Id == assignmentId && ers.SupplierId == user.Supplier.Id);

                if (assignment == null)
                {
                    return NotFound(new { message = "Excel talebi bulunamadý" });
                }

                // Mark as downloaded
                if (!assignment.Downloaded)
                {
                    assignment.Downloaded = true;
                    assignment.DownloadedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Get file
                var (fileData, contentType, fileName) = await _fileStorageService
                    .GetFileAsync(assignment.ExcelRequest.FilePath);

                return File(fileData, contentType, assignment.ExcelRequest.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading Excel file");
                return StatusCode(500, new { message = "Dosya indirilirken hata oluþtu" });
            }
        }

        /// <summary>
        /// Tedarikçi: Teklif Excel'i yükle
        /// </summary>
        [HttpPost("supplier/upload-offer")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> UploadSupplierOffer([FromForm] UploadSupplierOfferDto dto, [FromForm] IFormFile offerFile)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var user = await _context.Users
                    .Include(u => u.Supplier)
                    .FirstOrDefaultAsync(u => u.FullName == userEmail);

                if (user?.Supplier == null)
                {
                    return NotFound(new { message = "Tedarikçi bulunamadý" });
                }

                // Validate file
                if (offerFile == null || offerFile.Length == 0)
                {
                    return BadRequest(new { message = "Excel dosyasý seçilmedi" });
                }

                // Validate assignment
                var assignment = await _context.ExcelRequestSuppliers
                    .Include(ers => ers.ExcelRequest)
                    .FirstOrDefaultAsync(ers => ers.ExcelRequestId == dto.ExcelRequestId && 
                                                ers.SupplierId == user.Supplier.Id);

                if (assignment == null)
                {
                    return NotFound(new { message = "Size atanan bu Excel talebi bulunamadý" });
                }

                // Save offer file
                var (storedFileName, filePath, fileSize) = await _fileStorageService
                    .SaveExcelFileAsync(offerFile, $"SupplierOffers/{user.Supplier.Id}");

                // Create supplier offer
                var supplierOffer = new SupplierExcelOffer
                {
                    ExcelRequestId = dto.ExcelRequestId,
                    SupplierId = user.Supplier.Id,
                    OriginalFileName = offerFile.FileName,
                    StoredFileName = storedFileName,
                    FilePath = filePath,
                    FileSize = fileSize,
                    Status = OfferExcelStatus.Submitted,
                    Notes = dto.Notes,
                    UploadedDate = DateTime.Now
                };

                _context.SupplierExcelOffers.Add(supplierOffer);

                // Update assignment
                assignment.OfferUploaded = true;
                assignment.OfferUploadedDate = DateTime.Now;

                // Update Excel request status
                var totalAssignments = await _context.ExcelRequestSuppliers
                    .CountAsync(ers => ers.ExcelRequestId == dto.ExcelRequestId);
                var uploadedOffersCount = await _context.SupplierExcelOffers
                    .CountAsync(seo => seo.ExcelRequestId == dto.ExcelRequestId);

                var excelRequest = assignment.ExcelRequest;
                if (uploadedOffersCount + 1 >= totalAssignments)
                {
                    excelRequest.Status = ExcelRequestStatus.Completed;
                }
                else
                {
                    excelRequest.Status = ExcelRequestStatus.InProgress;
                }

                await _context.SaveChangesAsync();

                // Send notification to admin
                await SendNotificationToAdmin(excelRequest.Id, user.Supplier.CompanyName);

                _logger.LogInformation("Supplier {SupplierId} uploaded offer for Excel request {ExcelRequestId}", 
                    user.Supplier.Id, dto.ExcelRequestId);

                return Ok(new { success = true, message = "Teklif dosyasý baþarýyla yüklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading supplier offer");
                return StatusCode(500, new { success = false, message = "Teklif yüklenirken hata oluþtu: " + ex.Message });
            }
        }

        /// <summary>
        /// Admin: Excel taleplerini listele
        /// </summary>
        [HttpGet("admin/list")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<ExcelRequestDto>>> GetExcelRequests()
        {
            try
            {
                var requests = await _context.ExcelRequests
                    .Include(er => er.Site)
                    .Include(er => er.Employee).ThenInclude(e => e.User)
                    .Include(er => er.AssignedSuppliers).ThenInclude(ers => ers.Supplier).ThenInclude(s => s.User)
                    .Include(er => er.SupplierOffers).ThenInclude(seo => seo.Supplier).ThenInclude(s => s.User)
                    .OrderByDescending(er => er.UploadedDate)
                    .ToListAsync();

                var result = requests.Select(er => new ExcelRequestDto
                {
                    Id = er.Id,
                    SiteId = er.SiteId,
                    SiteName = er.Site?.Name ?? "",
                    EmployeeId = er.EmployeeId,
                    EmployeeName = er.Employee?.User?.FullName ?? "",
                    OriginalFileName = er.OriginalFileName,
                    StoredFileName = er.StoredFileName,
                    FileSize = er.FileSize,
                    UploadedDate = er.UploadedDate,
                    Status = er.Status.ToString(),
                    StatusValue = (int)er.Status,
                    Description = er.Description,
                    AssignedSuppliers = er.AssignedSuppliers.Select(ass => new AssignedSupplierDto
                    {
                        Id = ass.Id,
                        SupplierId = ass.SupplierId,
                        SupplierName = ass.Supplier?.User?.FullName ?? "",
                        CompanyName = ass.Supplier?.CompanyName ?? "",
                        AssignedDate = ass.AssignedDate,
                        Downloaded = ass.Downloaded,
                        DownloadedDate = ass.DownloadedDate,
                        OfferUploaded = ass.OfferUploaded,
                        OfferUploadedDate = ass.OfferUploadedDate
                    }).ToList(),
                    SupplierOffers = er.SupplierOffers.Select(so => new SupplierOfferDto
                    {
                        Id = so.Id,
                        SupplierId = so.SupplierId,
                        SupplierName = so.Supplier?.User?.FullName ?? "",
                        CompanyName = so.Supplier?.CompanyName ?? "",
                        OriginalFileName = so.OriginalFileName,
                        StoredFileName = so.StoredFileName,
                        FileSize = so.FileSize,
                        UploadedDate = so.UploadedDate,
                        Status = so.Status.ToString(),
                        StatusValue = (int)so.Status,
                        ApprovedByAdmin = so.ApprovedByAdmin,
                        ApprovedDate = so.ApprovedDate,
                        Notes = so.Notes
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Excel requests");
                return StatusCode(500, new { message = "Excel talepleri yüklenirken hata oluþtu" });
            }
        }

        [HttpGet("supplier/list")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<ExcelRequestDto>>> GetSupplierExcelRequests()
        {
            try
            {
                var requests = await _context.SupplierExcelOffers
                    .Include(er=> er.ExcelRequest)
                    .ThenInclude(er => er.Site)
                    .Include(er => er.ExcelRequest.AssignedSuppliers).ThenInclude(ers => ers.Supplier).ThenInclude(s => s.User)
                    .OrderByDescending(er => er.UploadedDate)
                    .ToListAsync();

                var result = requests.Select(er => new ExcelRequestDto
                {
                    Id = er.Id,
                    SiteId = er.ExcelRequest.Site.Id,
                    SiteName = er.ExcelRequest.Site?.Name ?? "",
                    OriginalFileName = er.ExcelRequest.OriginalFileName,
                    StoredFileName = er.ExcelRequest.StoredFileName,
                    FileSize = er.ExcelRequest.FileSize,
                    UploadedDate = er.UploadedDate,
                    Status = er.ExcelRequest.Status.ToString(),
                    StatusValue = (int)er.ExcelRequest.Status,
                    Description = er.ExcelRequest.Description,
                    AssignedSuppliers = er.ExcelRequest.AssignedSuppliers.Select(ass => new AssignedSupplierDto
                    {
                        Id = ass.Id,
                        SupplierId = ass.SupplierId,
                        SupplierName = ass.Supplier?.User?.FullName ?? "",
                        CompanyName = ass.Supplier?.CompanyName ?? "",
                        AssignedDate = ass.AssignedDate,
                        Downloaded = ass.Downloaded,
                        DownloadedDate = ass.DownloadedDate,
                        OfferUploaded = ass.OfferUploaded,
                        OfferUploadedDate = ass.OfferUploadedDate
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Excel requests");
                return StatusCode(500, new { message = "Excel talepleri yüklenirken hata oluþtu" });
            }
        }


        /// <summary>
        /// Admin: Orijinal Excel dosyasýný indir
        /// </summary>
        [HttpGet("admin/download/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadOriginalExcel(int id)
        {
            try
            {
                var excelRequest = await _context.ExcelRequests.FindAsync(id);
                if (excelRequest == null)
                {
                    return NotFound(new { message = "Excel talebi bulunamadý" });
                }

                var (fileData, contentType, fileName) = await _fileStorageService
                    .GetFileAsync(excelRequest.FilePath);

                return File(fileData, contentType, excelRequest.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading original Excel");
                return StatusCode(500, new { message = "Dosya indirilirken hata oluþtu" });
            }
        }

        /// <summary>
        /// Admin: Tedarikçi teklif dosyasýný indir
        /// </summary>
        [HttpGet("admin/download-offer/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadSupplierOfferExcel(int id)
        {
            try
            {
                var offer = await _context.SupplierExcelOffers.FindAsync(id);
                if (offer == null)
                {
                    return NotFound(new { message = "Teklif dosyasý bulunamadý" });
                }

                var (fileData, contentType, fileName) = await _fileStorageService
                    .GetFileAsync(offer.FilePath);

                return File(fileData, contentType, offer.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading supplier offer Excel");
                return StatusCode(500, new { message = "Dosya indirilirken hata oluþtu" });
            }
        }

        /// <summary>
        /// Admin: Excel talebini sil (orijinal dosya ve veritabaný kaydý)
        /// </summary>
        [HttpDelete("admin/delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteExcelRequest(int id)
        {
            try
            {
                var excelRequest = await _context.ExcelRequests
                    .Include(er => er.AssignedSuppliers)
                    .Include(er => er.SupplierOffers)
                    .FirstOrDefaultAsync(er => er.Id == id);

                if (excelRequest == null)
                {
                    return NotFound(new { message = "Excel talebi bulunamadý" });
                }

                // Delete original file from file system
                if (!string.IsNullOrEmpty(excelRequest.FilePath))
                {
                    await _fileStorageService.DeleteFileAsync(excelRequest.FilePath);
                }

                // Delete all supplier offer files
                foreach (var offer in excelRequest.SupplierOffers)
                {
                    if (!string.IsNullOrEmpty(offer.FilePath))
                    {
                        await _fileStorageService.DeleteFileAsync(offer.FilePath);
                    }
                }

                // Remove from database (cascade delete will handle related records)
                _context.ExcelRequests.Remove(excelRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Excel request {Id} and all related files deleted successfully", id);

                return Ok(new { success = true, message = "Excel talebi ve ilgili dosyalar baþarýyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Excel request {Id}", id);
                return StatusCode(500, new { success = false, message = "Excel talebi silinirken hata oluþtu: " + ex.Message });
            }
        }

        /// <summary>
        /// Admin: Tedarikçi teklif dosyasýný sil
        /// </summary>
        [HttpDelete("admin/delete-offer/{offerId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSupplierOffer(int offerId)
        {
            try
            {
                var offer = await _context.SupplierExcelOffers
                    .Include(seo => seo.ExcelRequest)
                    .FirstOrDefaultAsync(seo => seo.Id == offerId);

                if (offer == null)
                {
                    return NotFound(new { message = "Teklif dosyasý bulunamadý" });
                }

                // Delete file from file system
                if (!string.IsNullOrEmpty(offer.FilePath))
                {
                    await _fileStorageService.DeleteFileAsync(offer.FilePath);
                }

                var excelRequestId = offer.ExcelRequestId;

                // Remove from database
                _context.SupplierExcelOffers.Remove(offer);
                await _context.SaveChangesAsync();

                // Update Excel request status
                var remainingOffers = await _context.SupplierExcelOffers
                    .CountAsync(seo => seo.ExcelRequestId == excelRequestId);
                
                var excelRequest = await _context.ExcelRequests.FindAsync(excelRequestId);
                if (excelRequest != null)
                {
                    if (remainingOffers == 0)
                    {
                        excelRequest.Status = ExcelRequestStatus.AssignedToSuppliers;
                    }
                    else
                    {
                        excelRequest.Status = ExcelRequestStatus.InProgress;
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Supplier offer {OfferId} deleted successfully", offerId);

                return Ok(new { success = true, message = "Teklif dosyasý baþarýyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier offer {OfferId}", offerId);
                return StatusCode(500, new { success = false, message = "Teklif dosyasý silinirken hata oluþtu: " + ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task SendNotificationsToSuppliers(int excelRequestId, List<Supplier> suppliers, string siteName)
        {
            foreach (var supplier in suppliers)
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Yeni Excel Talebi",
                    message: $"{siteName} þantiyesi için yeni bir Excel talebi atandý. Lütfen indirip teklifinizi hazýrlayýn.",
                    type: NotificationType.ExcelRequestAssigned,
                    userId: supplier.UserId,
                    requestId: null,
                    offerId: null,
                    supplierId: supplier.Id
                );
            }
        }

        private async Task SendNotificationToAdmin(int excelRequestId, string companyName)
        {
            await _notificationService.CreateAdminNotificationAsync(
                title: "Tedarikçi Teklifi Yüklendi",
                message: $"{companyName} firmasý Excel teklif dosyasýný yükledi.",
                type: NotificationType.ExcelOfferUploaded,
                requestId: null,
                offerId: null,
                supplierId: null
            );
        }

        #endregion
    }
}
