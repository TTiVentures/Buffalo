using Buffalo.Models;
using Buffalo.Utils;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Buffalo.Implementations
{
	public class GoogleCloudStorage : IStorage, IDisposable
	{
		private readonly GoogleCredential googleCredential;
		private readonly StorageClient storageClient;
		private readonly string bucketName;

		public GoogleCloudStorage(IConfiguration configuration)
		{
			Dictionary<string, object> settings = configuration
			  .GetSection("GoogleCredentialFile")
			  .Get<Dictionary<string, object>>();
			string json = JsonConvert.SerializeObject(settings);

			googleCredential = GoogleCredential.FromJson(json);
			storageClient = StorageClient.Create(googleCredential);
			bucketName = configuration.GetValue<string>("GoogleCloudStorageBucket");
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