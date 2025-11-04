using Microsoft.AspNetCore.Mvc;
using SantiyeTalepWebUI.Models.DTOs;

namespace SantiyeTalepWebUI.Controllers
{
    /// <summary>
    /// Admin Controller - Excel Request Management
    /// </summary>
    public partial class AdminController
    {
        /// <summary>
        /// Excel talep yükleme
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadExcelRequest([FromForm] int siteId, [FromForm] int employeeId, 
            [FromForm] List<int> supplierIds, [FromForm] string? description, [FromForm] IFormFile excelFile)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuþ" });

            try
            {
                _logger.LogInformation("UploadExcelRequest called - SiteId: {SiteId}, EmployeeId: {EmployeeId}, Suppliers: {Count}", 
                    siteId, employeeId, supplierIds?.Count ?? 0);

                if (excelFile == null || excelFile.Length == 0)
                {
                    return Json(new { success = false, message = "Excel dosyasý seçilmedi" });
                }

                if (siteId <= 0 || employeeId <= 0)
                {
                    return Json(new { success = false, message = "Þantiye ve çalýþan seçimi zorunludur" });
                }

                if (supplierIds == null || !supplierIds.Any())
                {
                    return Json(new { success = false, message = "En az bir tedarikçi seçmelisiniz" });
                }

                _logger.LogInformation("Sending Excel file to API: {FileName}, Size: {Size} bytes", 
                    excelFile.FileName, excelFile.Length);

                // Read file content once into memory
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Prepare multipart form data
                using var content = new MultipartFormDataContent();
                
                // Add file from byte array (prevents multiple reads)
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    excelFile.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(fileContent, "excelFile", excelFile.FileName);
                
                // Add form fields
                content.Add(new StringContent(siteId.ToString()), "SiteId");
                content.Add(new StringContent(employeeId.ToString()), "EmployeeId");
                
                foreach (var supplierId in supplierIds)
                {
                    content.Add(new StringContent(supplierId.ToString()), "SupplierIds");
                }
                
                if (!string.IsNullOrEmpty(description))
                {
                    content.Add(new StringContent(description), "Description");
                }

                // Send to API (without retry since file is already in memory)
                var result = await _apiService.PostMultipartAsync<object>("api/ExcelRequest", content, token);
                
                if (result != null)
                {
                    _logger.LogInformation("Excel upload successful");
                    return Json(new { success = true, message = "Excel dosyasý baþarýyla yüklendi ve tedarikçilere gönderildi" });
                }

                _logger.LogWarning("Excel upload failed - API returned null");
                return Json(new { success = false, message = "Excel yüklenirken bir hata oluþtu" });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error uploading Excel request");
                
                string errorMessage = "Excel yüklenirken bir hata oluþtu";
                
                if (httpEx.Message.Contains("400"))
                {
                    errorMessage = "Geçersiz veri gönderildi. Lütfen tüm alanlarý kontrol edin.";
                }
                else if (httpEx.Message.Contains("413"))
                {
                    errorMessage = "Dosya boyutu çok büyük. Maksimum 10MB dosya yükleyebilirsiniz.";
                }
                
                return Json(new { success = false, message = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading Excel request");
                return Json(new { success = false, message = "Excel yüklenirken beklenmeyen bir hata oluþtu: " + ex.Message });
            }
        }

        /// <summary>
        /// Excel taleplerini listele
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetExcelRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuþ" });

            try
            {
                var excelRequests = await _apiService.GetAsync<List<object>>("api/ExcelRequest/admin/list", token) 
                    ?? new List<object>();
                
                return Json(new { success = true, data = excelRequests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Excel requests");
                return Json(new { success = false, message = "Excel talepleri yüklenirken hata oluþtu" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierExcelRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi dolmuþ" });

            try
            {
                var excelRequests = await _apiService.GetAsync<List<object>>("api/ExcelRequest/supplier/list", token)
                    ?? new List<object>();

                return Json(new { success = true, data = excelRequests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Excel requests");
                return Json(new { success = false, message = "Excel talepleri yüklenirken hata oluþtu" });
            }
        }

        /// <summary>
        /// Orijinal Excel dosyasýný indir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadOriginalExcel(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                _logger.LogInformation("Downloading original Excel file for request {Id}", id);

                // API'den dosyayý indir
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await httpClient.GetAsync($"{_apiService.BaseUrl}/api/ExcelRequest/admin/download/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download Excel file: {StatusCode}", response.StatusCode);
                    TempData["ErrorMessage"] = "Dosya indirilirken hata oluþtu";
                    return RedirectToAction("Requests");
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Dosya adýný düzgün þekilde al
                var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "TalepDosyasi.xlsx";

                // Týrnak iþaretlerini ve gereksiz karakterleri temizle
                fileName = fileName.Trim('"', '\'', '\\', ' ');

                var contentType = response.Content.Headers.ContentType?.MediaType ??
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                _logger.LogInformation("Excel file downloaded successfully: {FileName}, Size: {Size} bytes",
                    fileName, fileBytes.Length);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading original Excel");
                TempData["ErrorMessage"] = "Dosya indirilirken hata oluþtu";
                return RedirectToAction("Requests");
            }
        }


        /// <summary>
        /// Tedarikçi teklif dosyasýný indir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadSupplierOfferExcel(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                _logger.LogInformation("Downloading supplier offer Excel file {OfferId}", id);

                // API'den dosyayý indir
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                
                var response = await httpClient.GetAsync($"{_apiService.BaseUrl}/api/ExcelRequest/admin/download-offer/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download offer Excel file: {StatusCode}", response.StatusCode);
                    TempData["ErrorMessage"] = "Dosya indirilirken hata oluþtu";
                    return RedirectToAction("Requests");
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "TeklifDosyasi.xlsx";
                fileName = fileName.Trim('"', '\'', '\\', ' ');
                var contentType = response.Content.Headers.ContentType?.MediaType ?? 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                _logger.LogInformation("Offer Excel file downloaded successfully: {FileName}, Size: {Size} bytes", 
                    fileName, fileBytes.Length);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading supplier offer Excel");
                TempData["ErrorMessage"] = "Dosya indirilirken hata oluþtu";
                return RedirectToAction("Requests");
            }
        }

        /// <summary>
        /// Excel talebini sil (orijinal dosya ve tüm teklif dosyalarý)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExcelRequest(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                _logger.LogInformation("Deleting Excel request {Id}", id);

                var result = await _apiService.DeleteAsync($"api/ExcelRequest/admin/delete/{id}", token);
                
                if (result)
                {
                    _logger.LogInformation("Excel request {Id} deleted successfully", id);
                    return Json(new { success = true, message = "Excel talebi ve ilgili dosyalar baþarýyla silindi" });
                }

                return Json(new { success = false, message = "Excel talebi silinirken hata oluþtu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Excel request {Id}", id);
                return Json(new { success = false, message = "Excel talebi silinirken hata oluþtu: " + ex.Message });
            }
        }

        /// <summary>
        /// Tedarikçi teklif dosyasýný sil
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplierOfferExcel(int offerId)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                _logger.LogInformation("Deleting supplier offer {OfferId}", offerId);

                var result = await _apiService.DeleteAsync($"api/ExcelRequest/admin/delete-offer/{offerId}", token);
                
                if (result)
                {
                    _logger.LogInformation("Supplier offer {OfferId} deleted successfully", offerId);
                    return Json(new { success = true, message = "Teklif dosyasý baþarýyla silindi" });
                }

                return Json(new { success = false, message = "Teklif dosyasý silinirken hata oluþtu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier offer {OfferId}", offerId);
                return Json(new { success = false, message = "Teklif dosyasý silinirken hata oluþtu: " + ex.Message });
            }
        }
    }
}
