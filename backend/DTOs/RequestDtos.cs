using SantiyeTalepApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SantiyeTalepApi.DTOs
{
    public class CreateRequestDto
    {
        [Required(ErrorMessage = "Ürün açıklaması gereklidir")]
        [MinLength(3, ErrorMessage = "Ürün açıklaması en az 3 karakter olmalıdır")]
        public string ProductDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Teslim tipi seçimi gereklidir")]
        public DeliveryType DeliveryType { get; set; }

        [Required(ErrorMessage = "Açıklama gereklidir")]
        public string? Description { get; set; } = string.Empty;
    }

    public class RequestDto
    {
        public int Id { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public string Description { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public decimal? EstimatedCost { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime RequestDate { get; set; }

        // Employee info
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        // Site info
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;

        // Offers - reference to OfferDto from OfferDtos.cs
        public List<OfferDto> Offers { get; set; } = new List<OfferDto>();
    }

    // DTO for employee users - excludes offers for security
    public class EmployeeRequestDto
    {
        public int Id { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public string Description { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime CreatedDate { get; set; }

        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;

        // Offers are intentionally excluded - only admins can see offers
        public int OfferCount { get; set; }
    }

    public class ProductSearchDto
    {
        [Required]
        [MinLength(2, ErrorMessage = "Arama terimi en az 2 karakter olmalıdır")]
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Units { get; set; } = new List<string>();
    }

    // DTO for external API product mapping - handles flexible JSON structures
    public class ExternalProductDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ProductName { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        
        // Handle brand as flexible JsonElement to support both string and object formats
        [JsonPropertyName("brand")]
        public JsonElement? BrandElement { get; set; }
        
        public string? BrandName { get; set; }
        public string? Manufacturer { get; set; }
        public string? Category { get; set; }
        public string? Type { get; set; }
        public List<string>? Units { get; set; }
        public string? Unit { get; set; }

        // Helper property to extract brand string from JsonElement
        [JsonIgnore]
        public string? Brand
        {
            get
            {
                if (BrandElement.HasValue)
                {
                    var element = BrandElement.Value;
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        return element.GetString();
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        // Try to extract from common object properties
                        if (element.TryGetProperty("name", out var nameProperty))
                        {
                            return nameProperty.GetString();
                        }
                        if (element.TryGetProperty("brandName", out var brandNameProperty))
                        {
                            return brandNameProperty.GetString();
                        }
                        if (element.TryGetProperty("title", out var titleProperty))
                        {
                            return titleProperty.GetString();
                        }
                        // Fallback: return the whole object as string
                        return element.GetRawText();
                    }
                    else if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
                    {
                        // If it's an array, take the first element
                        var firstElement = element[0];
                        if (firstElement.ValueKind == JsonValueKind.String)
                        {
                            return firstElement.GetString();
                        }
                        else if (firstElement.ValueKind == JsonValueKind.Object)
                        {
                            if (firstElement.TryGetProperty("name", out var nameProperty))
                            {
                                return nameProperty.GetString();
                            }
                        }
                    }
                }

                // Fallback to other brand properties
                return BrandName ?? Manufacturer;
            }
        }
    }
}
