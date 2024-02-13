using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TTI.Buffalo.Models;
using FileInfo = TTI.Buffalo.Models.FileInfo;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace TTI.Buffalo.GoogleCloud;

public class GoogleCloudStorage : IStorage, IDisposable
{
    private readonly string _bucketName;
    private readonly StorageClient _storageClient;

    public GoogleCloudStorage(IOptions<GCSOptions> options)
    {
        var googleCredential = GoogleCredential.FromJson(options.Value.JsonCredentialsFile);
        _storageClient = StorageClient.Create(googleCredential);
        _bucketName = options.Value.StorageBucket ?? "default";
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async Task<FileData> RetrieveFileAsync(Guid id)
    {
        try
        {
            MemoryStream memoryStream = new();
            var obj = await _storageClient.DownloadObjectAsync(_bucketName, id.ToString(), memoryStream);
            // memoryStream.Position = 0;

            return new()
            {
                Id = Guid.Parse(obj.Name),
                Data = memoryStream,
                Metadata = obj.Metadata.ToDictionary(),
                MimeType = obj.ContentType
            };
        }
        catch (GoogleApiException ex)
        {
            if (ex.Error.Code == 404)
            {
                throw new FileNotFoundException(ex.Message);
            }

            throw new ApplicationException(ex.Message);
        }
        catch (TokenResponseException)
        {
            throw new UnauthorizedAccessException("Provided Cloud Storage credentials do not have access to this resource.");
        }
    }

    public async Task<List<FileInfo>> RetrieveFileListAsync()
    {
        var result = _storageClient.ListObjectsAsync(_bucketName);

        var response = new List<FileInfo>();

        await foreach (var blob in result)
        {
            if (!Guid.TryParse(blob.Name, out var id))
            {
                // _logger.LogWarning("Invalid file name: {FileName}", blob.Name);
                continue;
            }

            response.Add(new()
            {
                Id = id,
                Metadata = blob.Metadata
            });
        }

        return response;
    }

    public async Task DeleteFileAsync(Guid id, Action<IDictionary<string, string>> checkWritePermissions)
    {
        try
        {
            var obj = await _storageClient.GetObjectAsync(_bucketName, id.ToString());

            checkWritePermissions(obj.Metadata);

            await _storageClient.DeleteObjectAsync(_bucketName, id.ToString());
        }
        catch (GoogleApiException ex)
        {
            if (ex.Error.Code == 404)
            {
                throw new FileNotFoundException(ex.Message);
            }

            throw new ApplicationException(ex.Message);
        }
    }


    public async Task<string?> UpdateFileMetadataAsync(Guid fileId,
        Func<IDictionary<string, string>, IDictionary<string, string>> updateMetadata)
    {
        var obj = await _storageClient.GetObjectAsync(_bucketName, fileId.ToString());

        obj.Metadata = updateMetadata(obj.Metadata);

        var fileVisibility = GetPredefinedAcl(Enum.Parse<AccessLevels>(obj.Metadata[BuffaloMetadata.ReadAccessMode]));

        var objectFile = await _storageClient.UpdateObjectAsync(obj, new()
        {
            PredefinedAcl = fileVisibility
        });

        return fileVisibility == PredefinedObjectAcl.PublicRead ? objectFile.MediaLink : null;
    }

    public async Task<string> UploadFileAsync(IFormFile file, Guid fileId, IDictionary<string, string> metadata)
    {
        Object? obj = new()
        {
            Bucket = _bucketName,
            Name = fileId.ToString(),
            ContentType = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName),
            Metadata = metadata
        };

        // TODO: Add file hashing to metadata for not duplicating files
        // var al = MD5.Create();
        // await using var cs = new CryptoStream(file.OpenReadStream(), al, CryptoStreamMode.Read);

        await using var cs = file.OpenReadStream();
        var dataObject =
            await _storageClient.UploadObjectAsync(obj, cs, new()
            {
                PredefinedAcl = GetPredefinedAcl(Enum.Parse<AccessLevels>(metadata[BuffaloMetadata.ReadAccessMode]))
            });

        return "https://storage.cloud.google.com/" + dataObject.Bucket + "/" + dataObject.Name;
    }


    private static PredefinedObjectAcl GetPredefinedAcl(AccessLevels accessLevel)
    {
        return accessLevel == AccessLevels.Public ? PredefinedObjectAcl.PublicRead : PredefinedObjectAcl.Private;
    }
}