using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;
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

        public RequestController(ApplicationDbContext context, IMapper mapper, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<RequestController> logger)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("ProductApi");
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdClaim?.Value ?? "0");
            var employee = await _context.Employees
                .Include(e => e.Site)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
                return BadRequest(new { message = "Çalışan bilgisi bulunamadı" });

            var request = new Request
            {
                EmployeeId = employee.Id,
                SiteId = employee.SiteId,
                Title = model.Title,
                Description = model.Description,
                ProductDescription = model.ProductDescription,
                Unit = model.Unit,
                DeliveryType = model.DeliveryType,
                Category = model.Category,
                RequiredDate = model.RequiredDate,
                Status = RequestStatus.Open
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // Request'i detaylarıyla birlikte geri döndür
            var createdRequest = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.User)
                .Include(r => r.Site)
                .Include(r => r.Offers)
                    .ThenInclude(o => o.Supplier)
                        .ThenInclude(s => s.User)
                .FirstAsync(r => r.Id == request.Id);

            var requestDto = _mapper.Map<RequestDto>(createdRequest);
            return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, requestDto);
        }

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
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (employee == null)
                        return BadRequest(new { message = "Çalışan bilgisi bulunamadı" });
                    
                    query = query.Where(r => r.EmployeeId == employee.Id);
                    break;

                case "Supplier":
                    // Tedarikçiler sadece açık talepleri ve kendi tekliflerini görebilir
                    query = query.Where(r => r.Status == RequestStatus.Open || 
                                           r.Offers.Any(o => o.Supplier.UserId == userId));
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
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            switch (userRole)
            {
                case "Employee":
                    if (request.Employee.UserId != userId)
                        return Forbid();
                    break;

                case "Supplier":
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.UserId == userId);
                    if (supplier == null)
                        return BadRequest(new { message = "Tedarikçi bilgisi bulunamadı" });
                    
                    // Tedarikçi sadece açık talepleri veya kendi teklifini verdiği talepleri görebilir
                    if (request.Status != RequestStatus.Open && 
                        !request.Offers.Any(o => o.SupplierId == supplier.Id))
                        return Forbid();
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
                var products = await GetProductsFromApi(model.SearchTerm);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün arama sırasında bir hata oluştu");
                return StatusCode(500, new { message = "Ürün arama sırasında bir hata oluştu" });
            }
        }

        [HttpGet("/api/searchapi")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchApi([FromQuery] string query)
        {
            _logger.LogInformation("=== SEARCH API CALLED ===");
            _logger.LogInformation("Query parameter: '{Query}'", query);
            _logger.LogInformation("Query length: {Length}", query?.Length ?? 0);
            
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                _logger.LogWarning("Search API called with invalid query: '{Query}'", query);
                return BadRequest(new { message = "Arama terimi en az 2 karakter olmalıdır" });
            }

            try
            {
                _logger.LogInformation("=== STARTING PRODUCT SEARCH ===");
                _logger.LogInformation("Search query: '{Query}'", query);
                
                var products = await GetProductsFromApi(query);
                
                _logger.LogInformation("=== SEARCH COMPLETED ===");
                _logger.LogInformation("Found {Count} products for query: '{Query}'", products.Count, query);
                _logger.LogInformation("Products data: {Products}", System.Text.Json.JsonSerializer.Serialize(products));
                
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
            _logger.LogInformation("=== GET PRODUCTS FROM API STARTED ===");
            _logger.LogInformation("Search term: '{SearchTerm}'", searchTerm);
            
            try
            {
                // Get API URL from configuration
                var productApiUrl = _configuration["ExternalApis:ProductApiUrl"];
                _logger.LogInformation("Product API URL from config: '{Url}'", productApiUrl);
                
                if (string.IsNullOrEmpty(productApiUrl))
                {
                    _logger.LogWarning("=== PRODUCT API URL NOT CONFIGURED ===");
                    _logger.LogWarning("Using fallback mock data instead");
                    
                    // Return mock data for testing
                    var mockProducts = new List<ProductDto>
                    {
                        new ProductDto 
                        { 
                            Id = 1, 
                            Name = "Test Çimento", 
                            Description = "Test çimento açıklaması", 
                            Brand = "Test Marka",
                            Category = "Test Kategori",
                            Units = new List<string> { "Adet", "Çuval" }
                        },
                        new ProductDto 
                        { 
                            Id = 2, 
                            Name = "Test Demir", 
                            Description = "Test demir açıklaması", 
                            Brand = "Test Marka 2",
                            Category = "Test Kategori 2",
                            Units = new List<string> { "Ton", "Metre" }
                        }
                    };
                    
                    _logger.LogInformation("=== RETURNING MOCK DATA ===");
                    _logger.LogInformation("Mock products count: {Count}", mockProducts.Count);
                    _logger.LogInformation("Mock products: {Products}", System.Text.Json.JsonSerializer.Serialize(mockProducts));
                    
                    return mockProducts;
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

                var queryParams = $"?query={Uri.EscapeDataString(searchTerm)}&limit=20";
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
                                
                                var apiProducts = JsonSerializer.Deserialize<List<ExternalProductDto>>(productsJson, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
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
                            }
                        }
                    }
                }

                // If still no products found, add a debug entry to see what we're getting
                if (mappedProducts.Count == 0)
                {
                    _logger.LogWarning("=== NO PRODUCTS COULD BE PARSED ===");
                    _logger.LogWarning("Raw JSON: {Json}", jsonContent);
                    
                    // Fallback: create a mock product for testing
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        mappedProducts.Add(new ProductDto
                        {
                            Id = 999,
                            Name = $"Fallback Mock {searchTerm}",
                            Description = "Fallback test product - API parsing failed",
                            Brand = "Fallback Test Brand",
                            Category = "Fallback Test Category",
                            Units = new List<string> { "Adet" }
                        });
                        
                        _logger.LogInformation("Added fallback mock product");
                    }
                }

                _logger.LogInformation("=== FINAL RESULT ===");
                _logger.LogInformation("Successfully mapped {Count} products from external API", mappedProducts.Count);
                _logger.LogInformation("Final products: {Products}", System.Text.Json.JsonSerializer.Serialize(mappedProducts));
                
                return mappedProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== EXCEPTION IN GET PRODUCTS FROM API ===");
                _logger.LogError("Error occurred while calling external product API");
                return new List<ProductDto>();
            }
        }
    }
}
