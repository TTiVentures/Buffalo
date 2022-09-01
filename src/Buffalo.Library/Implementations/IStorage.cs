using Buffalo.Models;
using Microsoft.AspNetCore.Http;

namespace Buffalo.Implementations
{
    public interface IStorage
    {
        Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage, AccessLevels accessLevel, string? user);

        Task<FileData> RetrieveFileAsync(Guid id, string? user);

        Task DeleteFileAsync(Guid id, string? user);
    }
}