using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
using SantiyeTalepApi.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RequestController> _logger;
        private readonly INotificationService _notificationService;

        public RequestController(ApplicationDbContext context, IMapper mapper, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<RequestController> logger, INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("ProductApi");
            _logger = logger;
            _notificationService = notificationService;
        }

        [HttpPost]
        [Authorize(Roles = "Employee,Admin")] // Hem Employee hem Admin kabul et
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto model)
        {
            _logger.LogInformation("=== CREATE REQUEST STARTED ===");
            _logger.LogInformation("Request data: {Data}", System.Text.Json.JsonSerializer.Serialize(model));

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateRequest");
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogError("No user ID claim found in token");
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });
                }

                var userId = int.Parse(userIdClaim.Value);
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogInformation("Creating request for user ID: {UserId}, Role: {Role}", userId, userRole);

                Employee employee;

                if (userRole == "Admin")
                {
                    // Admin ise, model'den gelen EmployeeId'yi kullan
                    if (model.EmployeeId.HasValue)
                    {
                        employee = await _context.Employees
                            .Include(e => e.Site)
                            .Include(e => e.User)
                            .FirstOrDefaultAsync(e => e.Id == model.EmployeeId.Value);
                    }
                    else
                    {
                        return BadRequest(new { message = "Admin olarak talep oluştururken çalışan seçimi zorunludur" });
                    }
                }
                else
                {
                    // Employee ise, kendi bilgilerini kullan
                    employee = await _context.Employees
                        .Include(e => e.Site)
                        .Include(e => e.User)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                }

                if (employee == null)
                {
                    _logger.LogError("Employee not found");
                    return BadRequest(new { message = "Çalışan bilgisi bulunamadı" });
                }

                // Site kontrolü (Admin için)
                int siteId = userRole == "Admin" && model.SiteId.HasValue ? model.SiteId.Value : employee.SiteId ?? 0;

                if (siteId == 0)
                {
                    return BadRequest(new { message = "Şantiye bilgisi bulunamadı" });
                }

                var request = new Request
                {
                    EmployeeId = employee.Id,
                    SiteId = siteId,
                    ProductDescription = model.ProductDescription,
                    Quantity = model.Quantity,
                    DeliveryType = model.DeliveryType,
                    Description = model.Description,
                    Status = RequestStatus.Open,
                    RequestDate = DateTime.Now
                };

                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Request created with ID: {RequestId}", request.Id);

                // Bildirim gönder
                var notificationMessage = userRole == "Admin"
                    ? $"Admin tarafından {employee.User.FullName} adına talep oluşturuldu: {model.ProductDescription}"
                    : $"{employee.User.FullName} yeni bir talep oluşturdu: {model.ProductDescription}";

                await _notificationService.CreateAdminNotificationAsync(
                    "Yeni Talep Oluşturuldu",
                    notificationMessage,
                    NotificationType.NewRequest,
                    request.Id,
                    null,
                    null
                );

                var createdRequest = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.Site)
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                            .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(r => r.Id == request.Id);

                var requestDto = _mapper.Map<RequestDto>(createdRequest);
                _logger.LogInformation("=== CREATE REQUEST COMPLETED SUCCESSFULLY ===");

                return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, requestDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR IN CREATE REQUEST ===");
                return StatusCode(500, new
                {
                    message = "Talep oluşturulurken bir hata oluştu",
                    error = ex.Message
                });
            }
        }



        // Employee-specific endpoints that return EmployeeRequestDto without offers
        [HttpGet("employee")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetEmployeeRequests()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdClaim?.Value ?? "0");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
                return BadRequest(new { message = "Çalışan bilgisi bulunamadı" });

            var requests = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.User)
                .Include(r => r.Site)
                .Include(r => r.Offers) // Include for count only
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var employeeRequestDtos = requests.Select(r => new EmployeeRequestDto
            {
                Id = r.Id,
                ProductDescription = r.ProductDescription,
                Quantity = r.Quantity,
                DeliveryType = r.DeliveryType,
                Description = r.Description ?? "",
                Status = r.Status,
                RequestDate = r.RequestDate,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee.User.FullName,
                SiteId = r.SiteId,
                SiteName = r.Site.Name,
                OfferCount = r.Offers.Count() // Only provide count, not details
            }).ToList();

            return Ok(employeeRequestDtos);
        }

        [HttpGet("employee/{id}")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetEmployeeRequest(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdClaim?.Value ?? "0");

            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.User)
                .Include(r => r.Site)
                .Include(r => r.Offers) // Include for count only
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            // Verify employee owns this request
            if (request.Employee.UserId != userId)
                return Forbid();

            var employeeRequestDto = new EmployeeRequestDto
            {
                Id = request.Id,
                ProductDescription = request.ProductDescription,
                Quantity = request.Quantity,
                DeliveryType = request.DeliveryType,
                Description = request.Description ?? "",
                Status = request.Status,
                RequestDate = request.RequestDate,
                EmployeeId = request.EmployeeId,
                EmployeeName = request.Employee.User.FullName,
                SiteId = request.SiteId,
                SiteName = request.Site.Name,
                OfferCount = request.Offers.Count() // Only provide count, not details
            };

            return Ok(employeeRequestDto);
        }

        // Keep original endpoints for admin use
        [HttpGet]
        public async Task<IActionResult> GetRequests()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdClaim?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Request> query = _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.User)
                .Include(r => r.Site)
                .Include(r => r.Offers)
                    .ThenInclude(o => o.Supplier)
                        .ThenInclude(s => s.User);

            // Role-based filtering
            switch (userRole)
            {
                case "Employee":
                    // Redirect employees to use the employee-specific endpoint
                    return BadRequest(new { message = "Çalışanlar için employee endpoint kullanın" });

                case "Supplier":
                    // Tedarikçiler sadece kendilerine gönderilen talepleri görebilir
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.UserId == userId);

                    if (supplier == null)
                        return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });

                    // Sadece bu tedarikçiye bildirim gönderilen talepleri getir
                    var assignedRequestIds = await _context.Notifications
                        .Where(n => n.Type == NotificationType.RequestSentToSupplier 
                                   && n.UserId == userId 
                                   && n.RequestId.HasValue)
                        .Select(n => n.RequestId!.Value)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(r => assignedRequestIds.Contains(r.Id));
                    break;

                case "Admin":
                    // Admin tüm talepleri görebilir
                    break;

                default:
                    return Forbid();
            }

            var requests = await query.OrderByDescending(r => r.RequestDate).ToListAsync();
            var requestDtos = _mapper.Map<List<RequestDto>>(requests);
            return Ok(requestDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Redirect employees to use employee-specific endpoint
            if (userRole == "Employee")
            {
                return BadRequest(new { message = "Çalışanlar için employee/{id} endpoint kullanın" });
            }

            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.User)
                .Include(r => r.Site)
                .Include(r => r.Offers)
                    .ThenInclude(o => o.Supplier)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            // Access control
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdClaim?.Value ?? "0");

            switch (userRole)
            {
                case "Supplier":
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.UserId == userId);
                    if (supplier == null)
                        return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });
                    
                    // Tedarikçi sadece kendisine gönderilen talepleri görebilir
                    var hasAccess = await _context.Notifications
                        .AnyAsync(n => n.Type == NotificationType.RequestSentToSupplier 
                                      && n.UserId == userId 
                                      && n.RequestId == id);

                    if (!hasAccess)
                        return Forbid("Bu talebe erişim yetkiniz bulunmamaktadır");
                    break;

                case "Admin":
                    // Admin tüm talepleri görebilir
                    break;

                default:
                    return Forbid();
            }

            var requestDto = _mapper.Map<RequestDto>(request);
            return Ok(requestDto);
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdClaim?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Employee sadece kendi taleplerini iptal edebilir
            if (userRole == "Employee" && request.Employee.UserId != userId)
                return Forbid();

            if (request.Status == RequestStatus.Completed)
                return BadRequest(new { message = "Tamamlanmış talep iptal edilemez" });

            request.Status = RequestStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Talep iptal edildi" });
        }

        [HttpPost("search-products")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> SearchProducts([FromBody] ProductSearchDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get employee's site and associated brands
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userId = int.Parse(userIdClaim?.Value ?? "0");

                var employee = await _context.Employees
                    .Include(e => e.Site)
                        .ThenInclude(s => s.SiteBrands)
                            .ThenInclude(sb => sb.Brand)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                {
                    return BadRequest(new { message = "Çalışan bilgisi bulunamadı" });
                }

                // Extract brand IDs and names from employee's site
                var siteBrandIds = employee.Site.SiteBrands.Select(sb => sb.BrandId).ToList();
                var siteBrandNames = employee.Site.SiteBrands.Select(sb => sb.Brand.Name).ToList();

                _logger.LogInformation("Employee {UserId} searching products for site {SiteId} with brands: {BrandNames}", 
                    userId, employee.SiteId, string.Join(", ", siteBrandNames));

                var products = await GetProductsFromApiWithBrandFilter(model.SearchTerm, siteBrandIds, siteBrandNames);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün arama sırasında bir hata oluştu");
                return StatusCode(500, new { message = "Ürün arama sırasında bir hata oluştu" });
            }
        }

        [HttpGet("/api/searchapi")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> SearchApi([FromQuery] string query, [FromQuery] string brandIds = "")
        {
            _logger.LogInformation("=== SEARCH API CALLED ===");
            _logger.LogInformation("Query parameter: '{Query}'", query);
            _logger.LogInformation("BrandIds parameter: '{BrandIds}'", brandIds);
            _logger.LogInformation("User Role: '{Role}'", User.FindFirst(ClaimTypes.Role)?.Value);
            
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                _logger.LogWarning("Search API called with invalid query: '{Query}'", query);
                return BadRequest(new { message = "Arama terimi en az 2 karakter olmalıdır" });
            }

            try
            {
                _logger.LogInformation("=== STARTING PRODUCT SEARCH ===");
                _logger.LogInformation("Search query: '{Query}'", query);
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userId = int.Parse(userIdClaim?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                List<int> brandIdList = new List<int>();
                List<string> brandNameList = new List<string>();

                // Admin için brandIds parametresinden marka listesi al
                if (userRole == "Admin" && !string.IsNullOrEmpty(brandIds))
                {
                    _logger.LogInformation("Admin user - processing brandIds from parameter");
                    
                    var brandIdArray = brandIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var id in brandIdArray)
                    {
                        if (int.TryParse(id.Trim(), out var brandId))
                        {
                            brandIdList.Add(brandId);
                        }
                    }
                    
                    // Get brand names for filtering
                    if (brandIdList.Any())
                    {
                        var brands = await _context.Set<Brand>()
                            .Where(b => brandIdList.Contains(b.Id))
                            .Select(b => b.Name)
                            .ToListAsync();
                        brandNameList.AddRange(brands);
                        
                        _logger.LogInformation("Admin - Using brandIds: {BrandIds}, Brand names: {BrandNames}", 
                            brandIds, string.Join(", ", brandNameList));
                    }
                }
                // Employee için otomatik olarak şantiye markalarını al
                else if (userRole == "Employee")
                {
                    _logger.LogInformation("Employee user - fetching site brands automatically");
                    
                    var employee = await _context.Employees
                        .Include(e => e.Site)
                            .ThenInclude(s => s.SiteBrands)
                                .ThenInclude(sb => sb.Brand)
                        .FirstOrDefaultAsync(e => e.UserId == userId);

                    if (employee?.Site?.SiteBrands?.Any() == true)
                    {
                        // Use employee's site brands automatically
                        brandIdList = employee.Site.SiteBrands.Select(sb => sb.BrandId).ToList();
                        brandNameList = employee.Site.SiteBrands.Select(sb => sb.Brand.Name).ToList();
                        
                        _logger.LogInformation("Employee {UserId} from site {SiteId} ({SiteName}) - using site brands: {BrandNames}", 
                            userId, employee.SiteId, employee.Site.Name, string.Join(", ", brandNameList));
                    }
                    else
                    {
                        _logger.LogWarning("No site brands found for employee {UserId}", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("No brand filtering applied - userRole: {Role}, brandIds: {BrandIds}", userRole, brandIds);
                }
                
                var products = await GetProductsFromApiWithBrandFilter(query, brandIdList, brandNameList);
                
                _logger.LogInformation("=== SEARCH COMPLETED ===");
                _logger.LogInformation("Found {Count} products for query: '{Query}' with brand filter", products.Count, query);
                
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR IN SEARCH API ===");
                _logger.LogError("Error occurred while searching products for query: '{Query}'", query);
                return StatusCode(500, new { message = "Arama sırasında bir hata oluştu", error = ex.Message });
            }
        }

        private async Task<List<ProductDto>> GetProductsFromApi(string searchTerm)
        {
            return await GetProductsFromApiWithBrandFilter(searchTerm, new List<int>(), new List<string>());
        }

        private async Task<List<ProductDto>> GetProductsFromApiWithBrandFilter(string searchTerm, List<int> brandIds, List<string> brandNames)
        {
            _logger.LogInformation("=== GET PRODUCTS FROM API WITH BRAND FILTER STARTED ===");
            _logger.LogInformation("Search term: '{SearchTerm}'", searchTerm);
            _logger.LogInformation("Brand IDs: [{BrandIds}]", string.Join(", ", brandIds));
            _logger.LogInformation("Brand Names: [{BrandNames}]", string.Join(", ", brandNames));
            
            try
            {
                // Get API URL from configuration
                var productApiUrl = _configuration["ExternalApis:ProductApiUrl"];
                _logger.LogInformation("Product API URL from config: '{Url}'", productApiUrl);
                
                if (string.IsNullOrEmpty(productApiUrl))
                {
                    _logger.LogWarning("=== PRODUCT API URL NOT CONFIGURED ===");
                    _logger.LogWarning("Using fallback mock data instead");                    
                }

                var timeout = _configuration.GetValue<int>("ExternalApis:ProductApiTimeout", 30);
                _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                _logger.LogInformation("HTTP timeout set to: {Timeout} seconds", timeout);

                // Add API key if configured
                var apiKey = _configuration["ExternalApis:ProductApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                    _logger.LogInformation("API key configured and added to headers");
                }

                // Build query parameters with brand filtering
                var queryParams = $"?query={Uri.EscapeDataString(searchTerm)}&limit=20";
                
                // Add brand IDs to the query if provided
                if (brandIds.Any())
                {
                    queryParams += $"&brandIds={string.Join(",", brandIds)}";
                }

                var requestUrl = $"{productApiUrl}{queryParams}";

                _logger.LogInformation("=== CALLING EXTERNAL API ===");
                _logger.LogInformation("Request URL: {Url}", requestUrl);
                
                var response = await _httpClient.GetAsync(requestUrl);
                
                _logger.LogInformation("=== EXTERNAL API RESPONSE RECEIVED ===");
                _logger.LogInformation("Status Code: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Status Description: {StatusDescription}", response.ReasonPhrase);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("External API returned error status: {StatusCode} for URL: {Url}", 
                        response.StatusCode, requestUrl);
                    return new List<ProductDto>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("=== RAW API RESPONSE ===");
                _logger.LogInformation("Response length: {Length} characters", jsonContent.Length);
                _logger.LogInformation("Raw response: {Response}", jsonContent);

                // Try to determine the JSON structure
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("Empty response from external API");
                    return new List<ProductDto>();
                }

                // First, try to parse as JsonDocument to understand the structure
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;
                
                _logger.LogInformation("=== JSON STRUCTURE ANALYSIS ===");
                _logger.LogInformation("Root element type: {Type}", root.ValueKind);

                List<ProductDto> mappedProducts = new List<ProductDto>();

                // Handle different JSON structures
                if (root.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Processing as direct array of products");
                    
                    var apiProducts = JsonSerializer.Deserialize<List<ExternalProductDto>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiProducts != null)
                    {
                        _logger.LogInformation("Successfully deserialized {Count} products from array", apiProducts.Count);
                        
                        mappedProducts = apiProducts.Select(p => new ProductDto
                        {
                            Id = p.Id,
                            Name = p.Name ?? p.ProductName ?? p.Title ?? "",
                            Description = p.Description ?? "",
                            Brand = p.Brand ?? p.BrandName ?? p.Manufacturer ?? "",
                            Category = p.Category ?? p.Type ?? "",
                            Units = p.Units ?? (p.Unit != null ? new List<string> { p.Unit } : new List<string>())
                        }).ToList();
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    _logger.LogInformation("Processing as object wrapper");
                    
                    // Object wrapper - look for common property names
                    foreach (var property in root.EnumerateObject())
                    {
                        var propertyName = property.Name.ToLowerInvariant();
                        _logger.LogInformation("Found property: '{PropertyName}' of type: {Type}", propertyName, property.Value.ValueKind);
                        
                        // Common property names for product arrays
                        if ((propertyName == "data" || propertyName == "products" || propertyName == "items" || 
                             propertyName == "results" || propertyName == "content") && 
                            property.Value.ValueKind == JsonValueKind.Array)
                        {
                            try
                            {
                                var productsJson = property.Value.GetRawText();
                                _logger.LogInformation("Processing array property '{PropertyName}' with {Count} items", propertyName, property.Value.GetArrayLength());
                                
                                // First, try with JsonDocument to handle flexible parsing
                                var productElements = property.Value.EnumerateArray().ToList();
                                
                                foreach (var productElement in productElements)
                                {
                                    try
                                    {
                                        var productDto = ParseProductFromJsonElement(productElement);
                                        if (productDto != null)
                                        {
                                            mappedProducts.Add(productDto);
                                        }
                                    }
                                    catch (Exception productEx)
                                    {
                                        _logger.LogWarning(productEx, "Failed to parse individual product from JSON element: {Element}", productElement.GetRawText());
                                        // Continue with next product instead of failing completely
                                    }
                                }
                                
                                // If we successfully parsed any products, break
                                if (mappedProducts.Any())
                                {
                                    _logger.LogInformation("Successfully parsed {Count} products using JsonElement parsing", mappedProducts.Count);
                                    break;
                                }
                                
                                // Fallback: try the original JsonSerializer approach
                                var apiProducts = JsonSerializer.Deserialize<List<ExternalProductDto>>(productsJson, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });

                                if (apiProducts != null)
                                {
                                    mappedProducts = apiProducts.Select(p => new ProductDto
                                    {
                                        Id = p.Id,
                                        Name = p.Name ?? p.ProductName ?? p.Title ?? "",
                                        Description = p.Description ?? "",
                                        Brand = p.Brand ?? p.BrandName ?? p.Manufacturer ?? "",
                                        Category = p.Category ?? p.Type ?? "",
                                        Units = p.Units ?? (p.Unit != null ? new List<string> { p.Unit } : new List<string>())
                                    }).ToList();
                                    break;
                                }
                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogError(jsonEx, "JSON parsing error while processing property: {PropertyName}", propertyName);
                                // Continue to next property instead of failing completely
                            }
                        }
                    }
                }

                // Apply brand filtering if brand names are provided
                if (brandNames.Any() && mappedProducts.Any())
                {
                    var originalCount = mappedProducts.Count;
                    
                    // Filter products by brand names (case-insensitive)
                    mappedProducts = mappedProducts.Where(p => 
                        brandNames.Any(brandName => 
                            !string.IsNullOrEmpty(p.Brand) && 
                            p.Brand.Contains(brandName, StringComparison.OrdinalIgnoreCase)
                        )
                    ).ToList();
                    
                    _logger.LogInformation("=== BRAND FILTERING APPLIED ===");
                    _logger.LogInformation("Original product count: {OriginalCount}", originalCount);
                    _logger.LogInformation("Filtered product count: {FilteredCount}", mappedProducts.Count);
                    _logger.LogInformation("Filter brands: [{BrandNames}]", string.Join(", ", brandNames));
                }

                // If still no products found, add a debug entry to see what we're getting
                if (mappedProducts.Count == 0)
                {
                    _logger.LogWarning("=== NO PRODUCTS COULD BE PARSED OR MATCHED BRAND FILTER ===");
                    _logger.LogWarning("Raw JSON: {Json}", jsonContent);
                    
                    // Fallback: create a mock product for testing
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        var fallbackBrand = brandNames.FirstOrDefault() ?? "Test Brand";
                        mappedProducts.Add(new ProductDto
                        {
                            Id = 999,
                            Name = $"Fallback Mock {searchTerm}",
                            Description = brandNames.Any() ? 
                                $"Bu şantiyede kayıtlı markalar: {string.Join(", ", brandNames)}" : 
                                "Fallback test product - API parsing failed",
                            Brand = fallbackBrand,
                            Category = "Fallback Test Category",
                            Units = new List<string> { "Adet" }

                        });
                        
                        _logger.LogInformation("Added fallback mock product with brand filter");
                    }
                }

                _logger.LogInformation("=== FINAL RESULT ===");
                _logger.LogInformation("Successfully mapped {Count} products from external API with brand filtering", mappedProducts.Count);
                _logger.LogInformation("Final products: {Products}", System.Text.Json.JsonSerializer.Serialize(mappedProducts));
                
                return mappedProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== EXCEPTION IN GET PRODUCTS FROM API WITH BRAND FILTER ===");
                _logger.LogError("Error occurred while calling external product API");
                return new List<ProductDto>();
            }
        }

        private ProductDto? ParseProductFromJsonElement(JsonElement productElement)
        {
            try
            {
                var productDto = new ProductDto();
                
                // Parse ID
                if (productElement.TryGetProperty("id", out var idProp))
                {
                    productDto.Id = idProp.GetInt32();
                }

                // Parse Name (try multiple property names)
                var nameProps = new[] { "name", "productName", "title" };
                foreach (var nameProp in nameProps)
                {
                    if (productElement.TryGetProperty(nameProp, out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                    {
                        productDto.Name = nameElement.GetString() ?? "";
                        break;
                    }
                }

                // Parse Description
                if (productElement.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                {
                    productDto.Description = descProp.GetString() ?? "";
                }

                // Parse Brand (handle complex structures)
                var brandProps = new[] { "brand", "brandName", "manufacturer" };
                foreach (var brandProp in brandProps)
                {
                    if (productElement.TryGetProperty(brandProp, out var brandElement))
                    {
                        productDto.Brand = ExtractBrandFromJsonElement(brandElement);
                        if (!string.IsNullOrEmpty(productDto.Brand))
                        {
                            break;
                        }
                    }
                }

                // Parse Category
                var categoryProps = new[] { "category", "type" };
                foreach (var categoryProp in categoryProps)
                {
                    if (productElement.TryGetProperty(categoryProp, out var categoryElement) && categoryElement.ValueKind == JsonValueKind.String)
                    {
                        productDto.Category = categoryElement.GetString() ?? "";
                        break;
                    }
                }

                // Parse Units
                if (productElement.TryGetProperty("units", out var unitsProp) && unitsProp.ValueKind == JsonValueKind.Array)
                {
                    productDto.Units = unitsProp.EnumerateArray()
                        .Where(u => u.ValueKind == JsonValueKind.String)
                        .Select(u => u.GetString() ?? "")
                        .Where(u => !string.IsNullOrEmpty(u))
                        .ToList();
                }
                else if (productElement.TryGetProperty("unit", out var unitProp) && unitProp.ValueKind == JsonValueKind.String)
                {
                    var unit = unitProp.GetString();
                    if (!string.IsNullOrEmpty(unit))
                    {
                        productDto.Units = new List<string> { unit };
                    }
                }

                return productDto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse product from JsonElement");
                return null;
            }
        }

        private string ExtractBrandFromJsonElement(JsonElement brandElement)
        {
            try
            {
                if (brandElement.ValueKind == JsonValueKind.String)
                {
                    return brandElement.GetString() ?? "";
                }
                else if (brandElement.ValueKind == JsonValueKind.Object)
                {
                    // Try common object properties for brand name
                    var brandNameProps = new[] { "name", "brandName", "title", "label" };
                    foreach (var prop in brandNameProps)
                    {
                        if (brandElement.TryGetProperty(prop, out var nameProperty) && nameProperty.ValueKind == JsonValueKind.String)
                        {
                            var brandName = nameProperty.GetString();
                            if (!string.IsNullOrEmpty(brandName))
                            {
                                return brandName;
                            }
                        }
                    }
                    
                    // Fallback: try to get any string property
                    foreach (var property in brandElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            var value = property.Value.GetString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                return value;
                            }
                        }
                    }
                }
                else if (brandElement.ValueKind == JsonValueKind.Array && brandElement.GetArrayLength() > 0)
                {
                    // If it's an array, try to extract from the first element
                    return ExtractBrandFromJsonElement(brandElement[0]);
                }
                
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract brand from JsonElement");
                return "";
            }
        }

        [HttpGet("my-site")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMySite()
        {
            _logger.LogInformation("=== GET MY SITE STARTED ===");
            
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogError("No user ID claim found in token");
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });
                }

                var userId = int.Parse(userIdClaim.Value);
                _logger.LogInformation("Getting site for user ID: {UserId}", userId);

                var employee = await _context.Employees
                    .Include(e => e.Site)
                        .ThenInclude(s => s.SiteBrands)
                            .ThenInclude(sb => sb.Brand)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                {
                    _logger.LogError("Employee not found for user ID: {UserId}", userId);
                    return NotFound(new { message = "Çalışan bilgisi bulunamadı" });
                }

                if (employee.Site == null)
                {
                    _logger.LogWarning("Site not found for employee ID: {EmployeeId}", employee.Id);
                    return NotFound(new { message = "Şantiye bilgisi bulunamadı" });
                }

                var siteDto = _mapper.Map<SiteDto>(employee.Site);
                _logger.LogInformation("=== GET MY SITE COMPLETED SUCCESSFULLY ===");
                _logger.LogInformation("Site: {SiteName} (ID: {SiteId}) with {BrandCount} brands", 
                    siteDto.Name, siteDto.Id, siteDto.Brands.Count);

                return Ok(siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR IN GET MY SITE ===");
                return StatusCode(500, new { 
                    message = "Şantiye bilgisi alınırken bir hata oluştu", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("open")]
        [Authorize(Roles = "Supplier,Admin")]
        public async Task<IActionResult> GetOpenRequests()
        {
            _logger.LogInformation("=== GET OPEN REQUESTS STARTED ===");
            
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                _logger.LogInformation("User role: {Role}, UserId: {UserId}", userRole, userId);

                IQueryable<Request> query = _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.User)
                    .Include(r => r.Site)
                    .Include(r => r.Offers)
                        .ThenInclude(o => o.Supplier)
                            .ThenInclude(s => s.User)
                    .Where(r => r.Status == RequestStatus.Open || r.Status == RequestStatus.InProgress);

                // Tedarikçi ise sadece kendisine gönderilen talepleri görsün ve daha önce teklif verdiklerini hariç tutsun
                if (userRole == "Supplier")
                {
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.UserId == userId);

                    if (supplier == null)
                    {
                        _logger.LogError("Supplier not found for user ID: {UserId}", userId);
                        return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });
                    }

                    _logger.LogInformation("Found supplier: {SupplierId} for user {UserId}", supplier.Id, userId);

                    // Sadece bu tedarikçiye bildirim gönderilen talepleri getir
                    var assignedRequestIds = await _context.Notifications
                        .Where(n => n.Type == NotificationType.RequestSentToSupplier 
                                   && n.UserId == userId 
                                   && n.RequestId.HasValue)
                        .Select(n => n.RequestId!.Value)
                        .Distinct()
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} assigned requests for supplier {SupplierId}: [{RequestIds}]", 
                        assignedRequestIds.Count, supplier.Id, string.Join(", ", assignedRequestIds));

                    // Bu tedarikçinin daha önce teklif verdiği taleplerin ID'lerini al
                    var requestsWithOffers = await _context.Offers
                        .Where(o => o.SupplierId == supplier.Id)
                        .Select(o => o.RequestId)
                        .Distinct()
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} requests with existing offers from supplier {SupplierId}: [{RequestIds}]", 
                        requestsWithOffers.Count, supplier.Id, string.Join(", ", requestsWithOffers));

                    // Kendisine atanan ama henüz teklif vermediği talepleri getir
                    // SADECE OPEN durumundaki talepleri göster (InProgress olanları da teklif verilebilir olarak kabul et)
                    query = query.Where(r => assignedRequestIds.Contains(r.Id) && !requestsWithOffers.Contains(r.Id));

                    _logger.LogInformation("Final filter - assigned count: {AssignedCount}, with offers count: {OffersCount}", 
                        assignedRequestIds.Count, requestsWithOffers.Count);
                }

                var requests = await query
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} requests after filtering for user role {Role}", requests.Count, userRole);

                // Her bir sonucu da loglayalım
                foreach (var req in requests)
                {
                    _logger.LogInformation("Final result - Request ID: {Id}, Product: {Product}, Status: {Status}, Date: {Date}", 
                        req.Id, req.ProductDescription, req.Status, req.RequestDate);
                }

                var requestDtos = _mapper.Map<List<RequestDto>>(requests);
                
                _logger.LogInformation("=== GET OPEN REQUESTS COMPLETED SUCCESSFULLY ===");
                return Ok(requestDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR IN GET OPEN REQUESTS ===");
                return StatusCode(500, new { 
                    message = "Açık talepler yüklenirken bir hata oluştu", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("{requestId}/site-brands")]
        [Authorize(Roles = "Supplier,Admin")]
        public async Task<IActionResult> GetRequestSiteBrands(int requestId)
        {
            _logger.LogInformation("=== GET REQUEST SITE BRANDS STARTED ===");
            
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Tedarikçi ise sadece kendisine gönderilen taleplerin markalarına erişebilir
                if (userRole == "Supplier")
                {
                    var hasAccess = await _context.Notifications
                        .AnyAsync(n => n.Type == NotificationType.RequestSentToSupplier 
                                      && n.UserId == userId 
                                      && n.RequestId == requestId);

                    if (!hasAccess)
                    {
                        _logger.LogWarning("Supplier {UserId} tried to access brands for request {RequestId} without permission", userId, requestId);
                        return Forbid("Bu talebe erişim yetkiniz bulunmamaktadır");
                    }
                }

                var request = await _context.Requests
                    .Include(r => r.Site)
                        .ThenInclude(s => s.SiteBrands)
                            .ThenInclude(sb => sb.Brand)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request not found: {RequestId}", requestId);
                    return NotFound(new { message = "Talep bulunamadı" });
                }

                if (request.Site == null)
                {
                    _logger.LogWarning("Site not found for request: {RequestId}", requestId);
                    return NotFound(new { message = "Şantiye bilgisi bulunamadı" });
                }

                var brands = request.Site.SiteBrands
                    .Where(sb => sb.Brand.IsActive)
                    .Select(sb => new BrandDto
                    {
                        Id = sb.Brand.Id,
                        Name = sb.Brand.Name,
                        IsActive = sb.Brand.IsActive
                    })
                    .OrderBy(b => b.Name)
                    .ToList();

                _logger.LogInformation("Found {Count} brands for site {SiteId} (request {RequestId})", 
                    brands.Count, request.SiteId, requestId);

                return Ok(brands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR IN GET REQUEST SITE BRANDS ===");
                return StatusCode(500, new { 
                    message = "Şantiye markaları yüklenirken bir hata oluştu", 
                    error = ex.Message 
                });
            }
        }

        [HttpPut("{id}/mark-as-read")]
        [Authorize]
        public async Task<IActionResult> MarkRequestAsRead(int id)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userId = int.Parse(userIdClaim?.Value ?? "0");

                var request = await _context.Requests
                    .Include(r => r.Employee)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                    return NotFound(new { message = "Talep bulunamadı" });

                // Access control - sadece admin veya talep sahibi işaretleyebilir
                if (userRole == "Employee" && request.Employee.UserId != userId)
                    return Forbid("Bu talebe erişim yetkiniz bulunmamaktadır");

                if (userRole == "Supplier")
                {
                    // Tedarikçi sadece kendisine gönderilen talepleri işaretleyebilir
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.UserId == userId);
                    if (supplier == null)
                        return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });

                    var hasAccess = await _context.Notifications
                        .AnyAsync(n => n.Type == NotificationType.RequestSentToSupplier 
                                      && n.UserId == userId 
                                      && n.RequestId == id);

                    if (!hasAccess)
                        return Forbid("Bu talebe erişim yetkiniz bulunmamaktadır");
                }

                // Talebi okundu olarak işaretle
                request.IsRead = true;
                await _context.SaveChangesAsync();

                // İlgili bildirimleri de okundu olarak işaretle
                var relatedNotifications = await _context.Notifications
                    .Where(n => n.RequestId == id && n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in relatedNotifications)
                {
                    notification.IsRead = true;
                }

                if (relatedNotifications.Count > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(new { 
                    message = "Talep okundu olarak işaretlendi",
                    markedNotifications = relatedNotifications.Count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking request {RequestId} as read", id);
                return StatusCode(500, new { message = "Talep güncellenirken bir hata oluştu" });
            }
        }


      


    }
}
