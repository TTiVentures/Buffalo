using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TTI.Buffalo.Models;

namespace TTI.Buffalo
{
    public interface IStorage
	{
		Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage, AccessLevels accessLevel, ClaimsPrincipal? user, RequiredClaims? requiredClaims);
		Task<FileData> RetrieveFileAsync(Guid id, ClaimsPrincipal? user);
		Task DeleteFileAsync(Guid id, ClaimsPrincipal? user);
		Task<ObjectList> RetrieveFileListAsync();
    }
}