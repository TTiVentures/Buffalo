using Buffalo.Models;

namespace Buffalo.Implementations
{
	public interface IStorage
	{
		Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage, AccessModes accessMode, string? user);

		Task<FileData> RetrieveFileAsync(Guid id, string? user);

		Task DeleteFileAsync(Guid id, string? user);
	}
}