using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using TTI.Buffalo.Models;
using static System.Reflection.Metadata.BlobBuilder;

namespace TTI.Buffalo.GoogleCloud
{
    public class GoogleCloudStorage : IStorage, IDisposable
	{
		private readonly StorageClient _storageClient;
		private readonly string _bucketName;

		public GoogleCloudStorage(IOptions<GCSOptions> options)
		{
			GoogleCredential? googleCredential = GoogleCredential.FromJson(options.Value.JsonCredentialsFile);
			_storageClient = StorageClient.Create(googleCredential);
			_bucketName = options.Value.StorageBucket ?? "default";
		}

		public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage, 
			AccessLevels accessLevel, ClaimsPrincipal? user, RequiredClaims? requiredClaims)
		{

            string? userId = null;
            if (user != null && user.Identity != null)
            {
                userId = user.Identity.Name;
            }

            var al = MD5.Create();
            using var cs = new CryptoStream(file.OpenReadStream(), al, CryptoStreamMode.Read);

			Dictionary<string, string> meta = new ()
					{
						{ MetadataConst.BUFFALO_ACCESS_MODE, accessLevel.ToString() },
						{ MetadataConst.BUFFALO_USER_ID, userId ?? "" },
						{ MetadataConst.BUFFALO_FILENAME, file.FileName }
					};

			if (accessLevel == AccessLevels.CLAIMS)
			{
				if (requiredClaims != null)
				{
                    meta.Add(MetadataConst.BUFFALO_REQUIRED_CLAIMS, JsonSerializer.Serialize(requiredClaims));
                }
				else
				{
					throw new ArgumentException("RequiredClaims must not be empty in CLAIMS level access");
				}
				
			}

            Google.Apis.Storage.v1.Data.Object? obj = new()
			{
				Bucket = _bucketName,
				Name = fileNameForStorage,
				ContentType = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName),
				Metadata = meta
			};

			Google.Apis.Storage.v1.Data.Object dataObject =
				await _storageClient.UploadObjectAsync(obj, cs, new UploadObjectOptions
				{
					PredefinedAcl = accessLevel == AccessLevels.PUBLIC ? PredefinedObjectAcl.PublicRead : PredefinedObjectAcl.Private
				});

			return "https://storage.cloud.google.com/" + dataObject.Bucket + "/" + dataObject.Name;
		}

		public async Task DeleteFileAsync(Guid id, ClaimsPrincipal? user)
		{
			try
			{
				Google.Apis.Storage.v1.Data.Object? obj = _storageClient.GetObject(_bucketName, id.ToString());

				if (SecurityTool.VerifyAccess(user, obj.Metadata[MetadataConst.BUFFALO_USER_ID], 
					obj.Metadata[MetadataConst.BUFFALO_ACCESS_MODE], obj.Metadata[MetadataConst.BUFFALO_REQUIRED_CLAIMS]) == false)
				{
					throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
				}

				await _storageClient.DeleteObjectAsync(_bucketName, id.ToString());

			}
			catch (GoogleApiException ex)
			{
				if (ex.Error.Code == 404)
				{
					throw new FileNotFoundException(ex.Message);
				}
				else
				{
					throw new ApplicationException(ex.Message);

				}
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public async Task<FileData> RetrieveFileAsync(Guid id, ClaimsPrincipal? user)
		{
			try
			{
				Google.Apis.Storage.v1.Data.Object? obj = await _storageClient.GetObjectAsync(_bucketName, id.ToString());

				if (SecurityTool.VerifyAccess(user, obj.Metadata[MetadataConst.BUFFALO_USER_ID], 
					obj.Metadata[MetadataConst.BUFFALO_ACCESS_MODE], obj.Metadata[MetadataConst.BUFFALO_REQUIRED_CLAIMS]) == false)
				{
					throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
				}

				MemoryStream? memoryStream = new();

				// IDownloadProgress defined in Google.Apis.Download namespace
				Progress<IDownloadProgress>? progress = new(
					p => Console.WriteLine($"bytes: {p.BytesDownloaded}, status: {p.Status}")
				);

				// Download source object from bucket to local file system
				_ = await _storageClient.DownloadObjectAsync(_bucketName, id.ToString(), memoryStream, null, default, progress);

				memoryStream.Position = 0;

				return new FileData(memoryStream, obj.Metadata[MetadataConst.BUFFALO_FILENAME], obj.ContentType);

			}
			catch (GoogleApiException ex)
			{
				if (ex.Error.Code == 404)
				{
					throw new FileNotFoundException(ex.Message);
				}
				else
				{
					throw new ApplicationException(ex.Message);
				}
			}
			catch (TokenResponseException)
			{
				throw new UnauthorizedAccessException("Provided Cloud Storage credentials do not have access to this resource.");
			}
		}
        public async Task<ObjectList> RetrieveFileListAsync()
        {


			var result = _storageClient.ListObjectsAsync(_bucketName);
			int total = 0;

			var response = new ObjectList();

            await foreach (var blob in result)
            {
				response.Objects.Add(blob.Name);
				++total;
            }
			response.Total = total;

			return response;

        }
    }
}