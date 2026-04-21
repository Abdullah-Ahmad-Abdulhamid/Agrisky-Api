namespace AgriSky.API.Services
{
    public interface IStorageService
    {
        Task<string> SaveAsync(IFormFile file, string folderPath);
        void Delete(string filePath);
        bool Exists(string filePath);
        string GetFullPath(string relativePath);
    }
}