using Buffalo.Dto;
using Buffalo.Models;
using Buffalo.Utils;

namespace Buffalo.Implementations
{
    public class FileManager
    {
        private IStorage _cloudStorage;
        private FileContext _fileContext;

        public FileManager(IStorage cloudStorage, FileContext fileContext)
		{
			_cloudStorage = cloudStorage;
			_fileContext = fileContext;

		}

		public async Task<FileData> GetFile(Guid id, string? user)
		{


			var storedFile = _fileContext.Files.Find(id);

			if (storedFile == null)
			{
				throw new FileNotFoundException("File not exists.");
			}

			try
			{

				if (storedFile.AccessType == AccessModes.PROTECTED && user != null)
				{
					if (storedFile.UploadedBy != user)
					{
						throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
					}
				}

				var file = await _cloudStorage.RetrieveFileAsync(id);

				return new FileData
				{
					Data = file,
					MimeType = storedFile.ContentType,
					FileName = storedFile.FileName
				};

			}
			catch (Exception ex)
			{
				throw new FileNotFoundException(ex.Message);

			}
		}

		public async Task<FileDto> UploadFile(IFormFile file, string user, AccessModes accessMode = AccessModes.PRIVATE)
		{
			try
			{
				Console.WriteLine($"New picture request -> {file.FileName}");

				if (file.Length > 0)
				{
					Guid fileId = Guid.NewGuid();

					string imageUrl = await _cloudStorage.UploadFileAsync(file, fileId.ToString(), accessMode);

					string mime = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName);

					Models.File fileRecord = new()
					{
						CreatedOn = DateTime.UtcNow,
						FileId = fileId,
						FileName = file.FileName,
						ContentType = file.ContentType,
						AccessType = accessMode,
						UploadedBy = user
					};

					_fileContext.Files.Add(fileRecord);

					await _fileContext.SaveChangesAsync();

					return new FileDto
					{
						ContentType = mime,
						AccessType = accessMode,
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
			catch (Exception ex)
			{
				throw new ApplicationException($"Internal server error: {ex}");
			}
		}

		public async Task<bool> DeleteFile(Guid id, string? user)
		{

			var storedFile = _fileContext.Files.Find(id);

			if (storedFile == null)
			{
				throw new FileNotFoundException("File not exists.");
			}
			else
			{

				if (storedFile.AccessType == AccessModes.PROTECTED && user != null)
				{
					if (storedFile.UploadedBy != user)
					{
						throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
					}
				}

				_fileContext.Files.Remove(storedFile);

				try
				{
					await _cloudStorage.DeleteFileAsync(id.ToString());
				}

				catch (Exception e)
				{
					await _fileContext.SaveChangesAsync();
					return false;
				}

				await _fileContext.SaveChangesAsync();

				return true;

			}

		}

	}
}
