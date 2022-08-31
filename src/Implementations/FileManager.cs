using Buffalo.Dto;
using Buffalo.Models;
using Buffalo.Utils;

namespace Buffalo.Implementations
{
    public class FileManager
    {
        private IStorage _cloudStorage;

        public FileManager(IStorage cloudStorage)
		{
			_cloudStorage = cloudStorage;

		}

		public async Task<FileData> GetFile(Guid id, string? user)
		{
			try
			{

				var file = await _cloudStorage.RetrieveFileAsync(id, user);

				return file;

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

					string imageUrl = await _cloudStorage.UploadFileAsync(file, fileId.ToString(), accessMode, user);

					string mime = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName);

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
			try { 
			await _cloudStorage.DeleteFileAsync(id, user);
			return true;
			}
            catch (Exception ex)
            {
				throw new FileNotFoundException(ex.Message);

			}




		}

	}

}
