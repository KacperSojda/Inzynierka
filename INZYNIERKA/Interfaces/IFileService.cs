using Microsoft.AspNetCore.Http;

namespace INZYNIERKA.Services
{
    public interface IFileService
    {
        Task<(bool IsSuccess, string Result)> UploadAvatarAsync(IFormFile avatarFile);
    }
}