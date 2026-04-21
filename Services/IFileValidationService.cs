namespace AgriskyApi.Services
{
    public interface IFileValidationService
    {
        bool IsValidFile(IFormFile file, out string? errorMessage);

        /// <summary>
        /// Validates and saves a payment proof image to a location that is
        /// NOT served by the static files middleware (outside wwwroot).
        /// Returns the relative path stored in the database.
        /// </summary>
        Task<string> SaveProofAsync(IFormFile file, int orderId);
    }

    public class FileValidationService : IFileValidationService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileValidationService> _logger;

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "application/pdf"
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".pdf"
        };

        private const long MaxFileSize = 5 * 1024 * 1024;   // 5 MB
        private const long MinFileSize = 1024;               // 1 KB

        public FileValidationService(IWebHostEnvironment env, ILogger<FileValidationService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public bool IsValidFile(IFormFile file, out string? errorMessage)
        {
            errorMessage = null;

            if (file == null || file.Length == 0)
            {
                errorMessage = "File is empty.";
                return false;
            }

            if (file.Length < MinFileSize)
            {
                errorMessage = $"File size must be at least {MinFileSize} bytes.";
                return false;
            }

            if (file.Length > MaxFileSize)
            {
                errorMessage = $"File size cannot exceed {MaxFileSize / (1024 * 1024)} MB.";
                return false;
            }

            var mimeType = file.ContentType?.ToLowerInvariant();
            if (string.IsNullOrEmpty(mimeType) || !AllowedMimeTypes.Contains(mimeType))
            {
                errorMessage = "File type is not allowed.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                errorMessage = "File extension is not allowed.";
                return false;
            }

            if (!IsValidMagicNumber(file, extension))
            {
                errorMessage = "File content does not match its declared type.";
                return false;
            }

            return true;
        }

        public async Task<string> SaveProofAsync(IFormFile file, int orderId)
        {
            if (!IsValidFile(file, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            // ── Store proofs OUTSIDE wwwroot so they are NOT served by static files ──
            var contentRoot = _env.ContentRootPath;
            var proofsRoot = Path.Combine(contentRoot, "private", "proofs", orderId.ToString());

            if (!Directory.Exists(proofsRoot))
                Directory.CreateDirectory(proofsRoot);

            // GUID filename — prevents enumeration and path-traversal via FileName
            var safeExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{safeExtension}";
            var filePath = Path.Combine(proofsRoot, fileName);

            // ── Prevent path traversal ──────────────────────────────────────
            var fullPath = Path.GetFullPath(filePath);
            var fullRoot = Path.GetFullPath(proofsRoot);
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid file path detected.");

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Proof saved for order {OrderId}: {FileName}", orderId, fileName);

            // Return a logical path — NOT a web-accessible URL
            return $"private/proofs/{orderId}/{fileName}";
        }

        // ── Magic number validation ───────────────────────────────────────────
        private static bool IsValidMagicNumber(IFormFile file, string extension)
        {
            byte[] header = new byte[8];
            using var stream = file.OpenReadStream();
            var read = stream.Read(header, 0, header.Length);
            if (read < 4) return false;

            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8,
                ".png" => header[0] == 0x89 && header[1] == 0x50
                                              && header[2] == 0x4E && header[3] == 0x47,
                ".webp" => header[0] == 0x52 && header[1] == 0x49
                                              && header[2] == 0x46 && header[3] == 0x46,
                ".pdf" => header[0] == 0x25 && header[1] == 0x50
                                              && header[2] == 0x44 && header[3] == 0x46,
                _ => false
            };
        }
    }
}