using Buffalo.Models;
using Buffalo.Options;
using Buffalo.Utils;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

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

        public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage, AccessLevels accessLevel, string? user)
        {
            using MemoryStream memoryStream = new();
            await file.CopyToAsync(memoryStream);

            var obj = new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = bucketName,
                Name = fileNameForStorage,
                ContentType = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName),
                Metadata = new Dictionary<string, string>
                    {
                        { "buffalo_accessmode", accessLevel.ToString() },
                        { "buffalo_user", user ?? "" },
                        { "buffalo_filename", file.FileName }
                    }
            };

            Google.Apis.Storage.v1.Data.Object dataObject =
                await storageClient.UploadObjectAsync(obj, memoryStream, new UploadObjectOptions
                {
                    PredefinedAcl = (accessLevel == AccessLevels.PUBLIC) ? PredefinedObjectAcl.PublicRead : PredefinedObjectAcl.Private
                });

            return "https://storage.cloud.google.com/" + dataObject.Bucket + "/" + dataObject.Name;
        }

        public async Task DeleteFileAsync(Guid id, string? user)
        {
            try
            {

                var obj = storageClient.GetObject(bucketName, id.ToString());

                if (SecurityTool.VerifyAccess(user, obj.Metadata["buffalo_user"], obj.Metadata["buffalo_accessmode"]) == false)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
                }

                await storageClient.DeleteObjectAsync(bucketName, id.ToString());

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

        public async Task<FileData> RetrieveFileAsync(Guid id, string? user)
        {
            try
            {
                var obj = await storageClient.GetObjectAsync(bucketName, id.ToString());

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
                await storageClient.DownloadObjectAsync(bucketName, id.ToString(), memoryStream, null, default, progress);

                memoryStream.Position = 0;

                return new FileData(memoryStream, obj.Metadata["buffalo_filename"], obj.ContentType);

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
    }
}