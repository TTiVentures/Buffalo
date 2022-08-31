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

		public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage, AccessModes accessMode, string user)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				await file.CopyToAsync(memoryStream);

				var obj = new Google.Apis.Storage.v1.Data.Object
				{
					Bucket = bucketName,
					Name = fileNameForStorage,
					ContentType = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName),
					Metadata = new Dictionary<string, string>
					{
						{ "buffalo_accessmode", accessMode.ToString() },
						{ "buffalo_user", user },
						{ "buffalo_filename", file.FileName }
					}
				};

				Google.Apis.Storage.v1.Data.Object dataObject =
					await storageClient.UploadObjectAsync(obj, memoryStream, new UploadObjectOptions
					{
						PredefinedAcl = (accessMode == AccessModes.PUBLIC) ? PredefinedObjectAcl.PublicRead : PredefinedObjectAcl.Private
					});

				return "https://storage.cloud.google.com/" + dataObject.Bucket + "/" + dataObject.Name;
			}
		}

		public async Task DeleteFileAsync(Guid id, string user)
		{
			var obj = storageClient.GetObject(bucketName, id.ToString());

			if (SecurityTool.VerifyAccess(user, obj.Metadata["buffalo_user"], obj.Metadata["buffalo_accessmode"]) == false)
			{
				throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
			}

			await storageClient.DeleteObjectAsync(bucketName, id.ToString());
		}

		public void Dispose()
		{
			storageClient?.Dispose();
		}

		public async Task<FileData> RetrieveFileAsync(Guid id, string user)

		{

			var obj = storageClient.GetObject(bucketName, id.ToString());

			if (SecurityTool.VerifyAccess(user, obj.Metadata["buffalo_user"], obj.Metadata["buffalo_accessmode"]) == false)
			{
				throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
			}

			var memoryStream = new MemoryStream();
			
				// IDownloadProgress defined in Google.Apis.Download namespace
				var progress = new Progress<IDownloadProgress>(
					p => Console.WriteLine($"bytes: {p.BytesDownloaded}, status: {p.Status}")
				);

				// Download source object from bucket to local file system
				storageClient.DownloadObject(bucketName, id.ToString(), memoryStream, null, progress);

				memoryStream.Position = 0;

			return new FileData
			{
				Data = memoryStream,
				FileName = obj.Metadata["buffalo_filename"],
				MimeType = obj.ContentType
			};

			

		}

	}
}