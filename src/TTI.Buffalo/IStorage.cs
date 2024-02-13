using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TTI.Buffalo.Models;
using FileInfo = TTI.Buffalo.Models.FileInfo;

namespace TTI.Buffalo;

public interface IStorage
{
    Task<string> UploadFileAsync(IFormFile file, Guid fileId, IDictionary<string, string> metadata);
    Task<FileData> RetrieveFileAsync(Guid id);
    Task DeleteFileAsync(Guid id, Action<IDictionary<string, string>> checkWritePermissions);
    Task<List<FileInfo>> RetrieveFileListAsync();
    Task<string?> UpdateFileMetadataAsync(Guid fileId, Func<IDictionary<string, string>, IDictionary<string, string>> updateMetadata);
}