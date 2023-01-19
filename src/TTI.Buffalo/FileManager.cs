using Microsoft.AspNetCore.Http;
using TTI.Buffalo.Models;

namespace TTI.Buffalo
{
    public class FileManager
	{
		private readonly IStorage _storage;

		public FileManager(IStorage storage)
		{
			_storage = storage;

		}

		public async Task<FileData> GetFile(Guid id, string? user)
		{
			return await _storage.RetrieveFileAsync(id, user);
		}

		public async Task<FileDto> UploadFile(IFormFile file, string? user, AccessLevels accessLevel = AccessLevels.PRIVATE)
		{

			Console.WriteLine($"New picture request -> {file.FileName}");

			if (accessLevel == AccessLevels.PROTECTED && string.IsNullOrEmpty(user))
			{
				throw new ArgumentException("PROTECTED level access requires a non empty user");
			}

			if (file.Length > 0)
			{
				Guid fileId = Guid.NewGuid();

				string imageUrl = await _storage.UploadFileAsync(file, fileId.ToString(), accessLevel, user);

				string mime = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName) ?? "application/octet-stream";

				return new FileDto
				{
					ContentType = mime,
					AccessType = accessLevel,
					CreatedOn = DateTime.UtcNow,
					FileId = fileId,
					FileName = file.FileName,
					UploadedBy = user,
					ResourceUri = imageUrl

				};
			}
			else
			{
				throw new ArgumentException("File size can not be 0");
			}

		}

		public async Task<bool> DeleteFile(Guid id, string? user)
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
