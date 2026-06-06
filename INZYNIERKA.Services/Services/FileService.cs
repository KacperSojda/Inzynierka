using Microsoft.AspNetCore.Http;
using INZYNIERKA.Services.Interfaces;

namespace INZYNIERKA.Services.Services
{
    public class FileService : IFileService
    {
        public async Task<(bool Result, string ErrorMessage)> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File is empty.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return (false, "Unsupported file format. Allowed formats: .jpg, .jpeg, .png");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                return (false, "File is too large. Maximum size is 2MB.");
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);

                    byte[] fileBytes = memoryStream.ToArray();
                    string base64String = Convert.ToBase64String(fileBytes);
                    string dataUrl = $"data:{file.ContentType};base64,{base64String}";
                    return (true, dataUrl);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error processing file: {ex.Message}");
            }
        }
    }
}