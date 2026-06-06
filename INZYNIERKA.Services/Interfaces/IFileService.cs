using Microsoft.AspNetCore.Http;

namespace INZYNIERKA.Services.Interfaces
{
    public interface IFileService
    {
        Task<(bool Result, string ErrorMessage)> UploadFile(IFormFile file);
    }
}