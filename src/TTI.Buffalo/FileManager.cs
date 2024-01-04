using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using TTI.Buffalo.Models;

namespace TTI.Buffalo
{
    public class FileManager
	{
		private readonly IStorage _storage;
        private readonly ILogger<IStorage> _logger;

        public FileManager(IStorage storage, ILogger<IStorage> logger)
		{
			_storage = storage;
			_logger = logger;

		}

		public async Task<FileData> GetFile(Guid id, ClaimsPrincipal? user)
		{
			return await _storage.RetrieveFileAsync(id, user);
		}

		public async Task<FileDto> UploadFile(IFormFile file, ClaimsPrincipal? user, string? requiredClaims,  AccessLevels accessLevel = AccessLevels.USER_OWNED)
		{
			string? userId = null;
			RequiredClaims? reqClaims = null;
			if (user != null && user.Identity != null)
			{
                userId = user.Identity.Name;
            }

			_logger.LogDebug($"New file upload request: {file.FileName}");

			if (accessLevel != AccessLevels.PUBLIC && user == null)
			{
				throw new ArgumentException("USER_OWNED, ORGANIZATION_OWNED and CLAIMS level access requires a non empty user");
			}

			if (accessLevel == AccessLevels.CLAIMS)
			{
				if (!string.IsNullOrEmpty(requiredClaims)){

                    reqClaims = JsonSerializer.Deserialize<RequiredClaims>(requiredClaims);

					if (reqClaims == null)
					{
                        throw new ArgumentException("Failed to deserialize RequiredClaims");

                    }
                }
				else
				{
                    throw new ArgumentException("CLAIMS level access requires a non empty claims policy");

                }

            }

            if (file.Length > 0)
			{
				Guid fileId = Guid.NewGuid();

				string imageUrl = await _storage.UploadFileAsync(file, fileId.ToString(), accessLevel, user, reqClaims);

				string mime = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName) ?? "application/octet-stream";

				return new FileDto
				{
					ContentType = mime,
					AccessType = accessLevel,
					RequiredClaims = reqClaims,
					CreatedOn = DateTime.UtcNow,
					FileId = fileId,
					FileName = file.FileName,
					UploadedBy = userId,
					ResourceUri = imageUrl

				};
			}
			else
			{
				throw new ArgumentException("File size can not be 0");
			}

		}

		public async Task<bool> DeleteFile(Guid id, ClaimsPrincipal? user)
		{
			await _storage.DeleteFileAsync(id, user);
			return true;
		}

        public async Task<ObjectList> RetrieveFileListAsync()
        {
            return await _storage.RetrieveFileListAsync();
        }
    }
}
