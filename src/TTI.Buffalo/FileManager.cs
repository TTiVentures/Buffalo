using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TTI.Buffalo.Models;
using FileInfo = TTI.Buffalo.Models.FileInfo;

namespace TTI.Buffalo;

public class FileManager
{
    private readonly ILogger<IStorage> _logger;
    private readonly IStorage _storage;

    public FileManager(IStorage storage, ILogger<IStorage> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    private void CheckWritePermissions(IDictionary<string, string> existingMetadata, ClaimsPrincipal user)
    {
        var existingWriteClaims = existingMetadata[BuffaloMetadata.WriteClaims];
        var existingWriteAccessMode = Enum.Parse<AccessLevels>(existingMetadata[BuffaloMetadata.WriteAccessMode]);
        var existingUserId = Guid.Parse(existingMetadata[BuffaloMetadata.UserId]);

        if (!VerifyAccess(user, existingUserId, existingWriteAccessMode, existingWriteClaims.FromJson<SecurityClaims>()))
        {
            throw new UnauthorizedAccessException("User does not have access to update metadata");
        }
    }

    public async Task<FileData> GetFile(Guid id, ClaimsPrincipal? user)
    {
        return await _storage.RetrieveFileAsync(id, user);
    }

    public async Task<Guid> UploadFile(IFormFile file, ClaimsPrincipal user)
    {
        if (file.Length > 0)
        {
            var fileId = Guid.NewGuid();

            var metadata = new Dictionary<string, string>
            {
                [BuffaloMetadata.ReadAccessMode] = AccessLevels.UserOwned.ToString(),
                [BuffaloMetadata.WriteAccessMode] = AccessLevels.UserOwned.ToString(),
                [BuffaloMetadata.UserId] = GetUserClaimsId(user)
            };

            await _storage.UploadFileAsync(file, fileId, metadata);
            var mime = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName) ?? "application/octet-stream";

            return fileId;
        }

        throw new ArgumentException("File size can not be 0");
    }

    public async Task<bool> DeleteFile(Guid id, ClaimsPrincipal user)
    {
        await _storage.DeleteFileAsync(id, meta => CheckWritePermissions(meta, user));

        return true;
    }

    public async Task<List<FileInfo>> RetrieveFileListAsync(bool? includeMetadata, ClaimsPrincipal user)
    {
        return await _storage.RetrieveFileListAsync();
    }

    public async Task UpdateFileMetadata(Guid fileId,
        UpdateFileMetadataBody body,
        ClaimsPrincipal user)
    {
        Func<IDictionary<string, string>, IDictionary<string, string>> updateMetadata = existingMetadata =>
        {
            CheckWritePermissions(existingMetadata, user);

            existingMetadata[BuffaloMetadata.ReadAccessMode] = body.ReadAccessLevel.ToString();
            existingMetadata[BuffaloMetadata.ReadClaims] = body.ReadSecurityClaims.ToJson();
            existingMetadata[BuffaloMetadata.WriteAccessMode] = body.WriteAccessLevel.ToString();
            existingMetadata[BuffaloMetadata.WriteClaims] = body.WriteSecurityClaims.ToJson();
            existingMetadata[BuffaloMetadata.UserId] = GetUserClaimsId(user);
            return existingMetadata;
        };

        await _storage.UpdateFileMetadataAsync(fileId, updateMetadata);
    }


    private static string GetUserClaimsId(ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value ?? "";
    }

    public static bool VerifyAccess(ClaimsPrincipal requestUser, Guid storedUser, AccessLevels storedAccessLevel,
        SecurityClaims? storedRequiredClaims = null)
    {
        if (!Guid.TryParse(requestUser.Identity?.Name, out var userId))
        {
            throw new ArgumentException("Invalid user id");
        }

        if (requestUser.Claims.Any(x => x is { Type: "role", Value: "system_admin" }))
        {
            return true;
        }

        return storedAccessLevel switch
        {
            AccessLevels.Public => true,
            AccessLevels.AuthenticatedUser => requestUser != null,
            AccessLevels.UserOwned => userId == storedUser,
            AccessLevels.Claims => storedRequiredClaims is not null && storedRequiredClaims.CheckClaims(requestUser.Claims),
            _ => false
        };
    }
}

public static class ObjectExtensions
{
    public static string ToJson(this object? obj)
    {
        return JsonSerializer.Serialize(obj);
    }

    public static T FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json) ?? throw new ApplicationException("Failed to deserialize json");
    }
}