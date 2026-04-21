namespace AgriSky.API.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalStorageService> _logger;

        public LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Saves a file to wwwroot/{folderPath}
        /// </summary>
        public async Task<string> SaveAsync(IFormFile file, string folderPath)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            // Validate file size (5 MB limit)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
                throw new InvalidOperationException("File size exceeds 5 MB limit.");

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, folderPath);

            // Ensure directory exists
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File saved: {FilePath}", filePath);

            // Return relative path for storage in DB
            return $"/{folderPath}/{fileName}";
        }

        /// <summary>
        /// Deletes a file by relative path
        /// </summary>
        public void Delete(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return;

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, relativePath.TrimStart('/'));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        public bool Exists(string relativePath)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, relativePath.TrimStart('/'));
            return File.Exists(filePath);
        }

        /// <summary>
        /// Gets the full file path from a relative path
        /// </summary>
        public string GetFullPath(string relativePath)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            return Path.Combine(webRoot, relativePath.TrimStart('/'));
        }
    }
}