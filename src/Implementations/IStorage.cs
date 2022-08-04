using Buffalo.Models;

namespace Buffalo.Implementations
{
	public interface IStorage
	{
		Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage, AccessModes accessMode);

		Task<Stream> RetrieveFileAsync(Guid id);

		Task DeleteFileAsync(string fileNameForStorage);
	}
}