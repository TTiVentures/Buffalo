using Buffalo.Models;
using Buffalo.Options;
using Buffalo.Utils;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Buffalo.Implementations
{
	public class GoogleCloudStorage : IStorage, IDisposable
	{
		private readonly StorageClient storageClient;
		private readonly string bucketName;

		public GoogleCloudStorage(IOptions<GCSOptions> options)
		{

			var googleCredential = GoogleCredential.FromJson(options.Value.JsonCredentialsFile);
			storageClient = StorageClient.Create(googleCredential);
			bucketName = options.Value.StorageBucket ?? "default";
		}

		public async Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage, AccessModes accessMode)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				await imageFile.CopyToAsync(memoryStream);
				Google.Apis.Storage.v1.Data.Object dataObject =
					await storageClient.UploadObjectAsync(bucketName, fileNameForStorage,
					imageFile.ContentType ?? MimeTypeTool.GetMimeType(imageFile.FileName), memoryStream, new UploadObjectOptions
					{
						PredefinedAcl = (accessMode == AccessModes.PUBLIC) ? PredefinedObjectAcl.PublicRead : PredefinedObjectAcl.Private
					});

				return "https://storage.cloud.google.com/" + dataObject.Bucket + "/" + dataObject.Name;
			}
		}

		public async Task DeleteFileAsync(string fileNameForStorage)
		{
			await storageClient.DeleteObjectAsync(bucketName, fileNameForStorage);
		}

		public void Dispose()
		{
			storageClient?.Dispose();
		}

		public async Task<Stream> RetrieveFileAsync(Guid id)

		{
			var memoryStream = new MemoryStream();
			
				// IDownloadProgress defined in Google.Apis.Download namespace
				var progress = new Progress<IDownloadProgress>(
					p => Console.WriteLine($"bytes: {p.BytesDownloaded}, status: {p.Status}")
				);

				// Download source object from bucket to local file system
				storageClient.DownloadObject(bucketName, id.ToString(), memoryStream, null, progress);

				memoryStream.Position = 0;

				return memoryStream;

			

		}

	}
}