using SantiyeTalepApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SantiyeTalepApi.DTOs
{
    public class CreateRequestDto
    {
        [Required]
        public string ProductDescription { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public DeliveryType DeliveryType { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public decimal? EstimatedCost { get; set; }
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
        
        // Offers
        public List<OfferDto> Offers { get; set; } = new();
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
        public int OfferCount { get; set; } // Only show count for employees
    }

    public class OfferDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int SupplierId { get; set; }
        public decimal Price { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public OfferStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Supplier info
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierContact { get; set; } = string.Empty;
        
        // Request info
        public string RequestTitle { get; set; } = string.Empty;
    }

    public class CreateOfferDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 365, ErrorMessage = "Teslimat süresi 1-365 gün arasında olmalıdır")]
        public int DeliveryDays { get; set; }

        public string Description { get; set; } = string.Empty;
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

    public class ExternalProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Brand { get; set; } = string.Empty;

        [JsonConverter(typeof(FlexibleStringConverter))]
        public string BrandName { get; set; } = string.Empty;

        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Manufacturer { get; set; } = string.Empty;

        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Category { get; set; } = string.Empty;

        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Type { get; set; } = string.Empty;

        public List<string> Units { get; set; } = new List<string>();
        public string Unit { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString() ?? string.Empty;
                case JsonTokenType.StartObject:
                    // Object ise, name property'sini ara veya toString yap
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        var root = doc.RootElement;

                        // Önce yaygın property isimlerini dene
                        if (root.TryGetProperty("name", out var nameProperty))
                            return nameProperty.GetString() ?? string.Empty;

                        if (root.TryGetProperty("title", out var titleProperty))
                            return titleProperty.GetString() ?? string.Empty;

                        if (root.TryGetProperty("value", out var valueProperty))
                            return valueProperty.GetString() ?? string.Empty;

                        // Eğer hiçbiri yoksa boş string döndür
                        return string.Empty;
                    }
                case JsonTokenType.Null:
                    return string.Empty;
                default:
                    return reader.GetString() ?? string.Empty;
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
