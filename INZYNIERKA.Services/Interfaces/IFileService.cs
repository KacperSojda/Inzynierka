using Microsoft.AspNetCore.Http;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IFileService
    {
        Task<(bool IsSuccess, string Result)> UploadAvatar(IFormFile avatarFile);
    }
}