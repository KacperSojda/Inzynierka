using Microsoft.AspNetCore.Http;
using INZYNIERKA.Services.Interfaces;

namespace INZYNIERKA.Services.Services
{
    public class FileService : IFileService
    {
        public async Task<(bool IsSuccess, string Result)> UploadAvatarAsync(IFormFile avatarFile)
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                return (false, "Nie wybrano pliku.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return (false, "Nieobsługiwany format pliku. Dozwolone formaty: .jpg, .jpeg, .png");
            }

            if (avatarFile.Length > 2 * 1024 * 1024)
            {
                return (false, "Plik jest zbyt duży. Maksymalny rozmiar to 2MB.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            return (true, $"/uploads/avatars/{fileName}");
        }
    }
}