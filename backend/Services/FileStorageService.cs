namespace SantiyeTalepApi.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Excel dosyasýný yükler ve dosya bilgilerini döner
        /// </summary>
        Task<(string StoredFileName, string FilePath, long FileSize)> SaveExcelFileAsync(IFormFile file, string subfolder);

        /// <summary>
        /// Dosyayý indirir
        /// </summary>
        Task<(byte[] FileData, string ContentType, string FileName)> GetFileAsync(string filePath);

        /// <summary>
        /// Dosyayý siler
        /// </summary>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Dosya var mý kontrol eder
        /// </summary>
        Task<bool> FileExistsAsync(string filePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseUploadPath;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
        {
            _baseUploadPath = Path.Combine(environment.ContentRootPath, "Uploads", "ExcelFiles");
            _logger = logger;

            // Ensure base directory exists
            if (!Directory.Exists(_baseUploadPath))
            {
                Directory.CreateDirectory(_baseUploadPath);
                _logger.LogInformation("Created base upload directory: {Path}", _baseUploadPath);
            }
        }

        public async Task<(string StoredFileName, string FilePath, long FileSize)> SaveExcelFileAsync(IFormFile file, string subfolder)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("Geçersiz dosya");
                }

                // Validate file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    throw new ArgumentException("Sadece .xlsx ve .xls dosyalarý kabul edilir");
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    throw new ArgumentException("Dosya boyutu 10MB'dan küçük olmalýdýr");
                }

                // Create subfolder path
                var uploadPath = Path.Combine(_baseUploadPath, subfolder);
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File saved successfully: {FileName} -> {FilePath}", file.FileName, filePath);

                // Return relative path from base upload folder
                var relativePath = Path.Combine(subfolder, uniqueFileName);
                
                return (uniqueFileName, relativePath, file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", file?.FileName);
                throw;
            }
        }

        public async Task<(byte[] FileData, string ContentType, string FileName)> GetFileAsync(string relativePath)
        {
            try
            {
                var fullPath = Path.Combine(_baseUploadPath, relativePath);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Dosya bulunamadý: {relativePath}");
                }

                var fileData = await File.ReadAllBytesAsync(fullPath);
                var extension = Path.GetExtension(fullPath).ToLowerInvariant();
                
                var contentType = extension switch
                {
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".xls" => "application/vnd.ms-excel",
                    _ => "application/octet-stream"
                };

                var fileName = Path.GetFileName(fullPath);

                _logger.LogInformation("File retrieved successfully: {FilePath}", relativePath);

                return (fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file: {FilePath}", relativePath);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string relativePath)
        {
            try
            {
                var fullPath = Path.Combine(_baseUploadPath, relativePath);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation("File deleted successfully: {FilePath}", relativePath);
                    return true;
                }

                _logger.LogWarning("File not found for deletion: {FilePath}", relativePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", relativePath);
                throw;
            }
        }

        public Task<bool> FileExistsAsync(string relativePath)
        {
            try
            {
                var fullPath = Path.Combine(_baseUploadPath, relativePath);
                return Task.FromResult(File.Exists(fullPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {FilePath}", relativePath);
                return Task.FromResult(false);
            }
        }
    }
}
