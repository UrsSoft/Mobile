using Microsoft.AspNetCore.Mvc;

namespace SantiyeTalepWebUI.Controllers
{
    /// <summary>
    /// Supplier Controller - Excel Request Management
    /// </summary>
    public partial class SupplierController
    {
        /// <summary>
        /// Excel talepleri sayfasý
        /// </summary>
        [HttpGet]
        public IActionResult ExcelRequests()
        {
            return View();
        }

        /// <summary>
        /// Tedarikçiye atanan Excel taleplerini getir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAssignedExcelRequests()
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                _logger.LogInformation("Getting assigned Excel requests for supplier");

                var excelRequests = await _apiService.GetAsync<List<object>>(
                    "api/ExcelRequest/supplier/assigned", token);

                return Json(new { success = true, data = excelRequests ?? new List<object>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned Excel requests");
                return Json(new { success = false, message = "Excel talepleri yüklenirken hata oluþtu" });
            }
        }

        /// <summary>
        /// Excel dosyasýný indir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadExcelFile(int id)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                _logger.LogInformation("Downloading Excel file, assignment ID: {Id}", id);

                // API'den dosyayý indir
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await httpClient.GetAsync(
                    $"{_apiService.BaseUrl}/api/ExcelRequest/supplier/download/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download Excel file: {StatusCode}", response.StatusCode);
                    TempData["ErrorMessage"] = "Dosya indirilirken hata oluþtu";
                    return RedirectToAction("ExcelRequests");
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "TalepDosyasi.xlsx";
                var contentType = response.Content.Headers.ContentType?.MediaType ??
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                _logger.LogInformation("Excel file downloaded successfully: {FileName}, Size: {Size} bytes",
                    fileName, fileBytes.Length);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading Excel file");
                TempData["ErrorMessage"] = "Dosya indirilirken hata oluþtu";
                return RedirectToAction("ExcelRequests");
            }
        }

        /// <summary>
        /// Teklif Excel'i yükle
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadOfferExcel([FromForm] int excelRequestId, 
            [FromForm] IFormFile offerFile, [FromForm] string? notes)
        {
            var token = _authService.GetStoredToken();
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Oturum süresi doldu" });

            try
            {
                _logger.LogInformation("Uploading offer Excel for request {ExcelRequestId}", excelRequestId);

                if (offerFile == null || offerFile.Length == 0)
                {
                    return Json(new { success = false, message = "Excel dosyasý seçilmedi" });
                }

                // Validate file size (10MB)
                if (offerFile.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Dosya boyutu 10MB'dan küçük olmalýdýr" });
                }

                // Validate file extension
                var extension = Path.GetExtension(offerFile.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    return Json(new { success = false, message = "Sadece .xlsx ve .xls dosyalarý kabul edilir" });
                }

                // Read file content into memory
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await offerFile.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Prepare multipart form data
                using var content = new MultipartFormDataContent();

                // Add file from byte array
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    offerFile.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(fileContent, "offerFile", offerFile.FileName);

                // Add form fields
                content.Add(new StringContent(excelRequestId.ToString()), "ExcelRequestId");

                if (!string.IsNullOrEmpty(notes))
                {
                    content.Add(new StringContent(notes), "Notes");
                }

                _logger.LogInformation("Sending offer Excel to API: {FileName}, Size: {Size} bytes",
                    offerFile.FileName, offerFile.Length);

                // Send to API
                var result = await _apiService.PostMultipartAsync<object>(
                    "api/ExcelRequest/supplier/upload-offer", content, token);

                if (result != null)
                {
                    _logger.LogInformation("Offer Excel uploaded successfully");
                    return Json(new { success = true, message = "Teklif dosyanýz baþarýyla yüklendi ve admin'e bildirim gönderildi" });
                }

                _logger.LogWarning("Offer Excel upload failed - API returned null");
                return Json(new { success = false, message = "Teklif yüklenirken bir hata oluþtu" });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error uploading offer Excel");

                string errorMessage = "Teklif yüklenirken bir hata oluþtu";

                if (httpEx.Message.Contains("400"))
                {
                    errorMessage = "Geçersiz veri gönderildi. Lütfen dosyayý kontrol edin.";
                }
                else if (httpEx.Message.Contains("413"))
                {
                    errorMessage = "Dosya boyutu çok büyük. Maksimum 10MB dosya yükleyebilirsiniz.";
                }

                return Json(new { success = false, message = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading offer Excel");
                return Json(new { success = false, message = "Teklif yüklenirken beklenmeyen bir hata oluþtu: " + ex.Message });
            }
        }
    }
}
