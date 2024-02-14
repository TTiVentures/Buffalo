using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TTI.Buffalo.Models;

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

    private void CheckReadPermissions(IDictionary<string, string> existingMetadata, ClaimsPrincipal? user)
    {
        if (!HasReadPermissions(existingMetadata, user))
        {
            throw new UnauthorizedAccessException("User does not have read access to read metadata");
        }
    }

    private bool HasReadPermissions(IDictionary<string, string> existingMetadata, ClaimsPrincipal? user)
    {
        existingMetadata.TryGetValue(BuffaloMetadata.ReadClaims, out var readClaimsJson);
        var existingReadClaims = readClaimsJson.FromJson<SecurityClaims>();
        var existingReadAccessMode = Enum.Parse<AccessLevels>(existingMetadata[BuffaloMetadata.ReadAccessMode]);
        var existingUserId = Guid.Parse(existingMetadata[BuffaloMetadata.UserId]);

        return VerifyAccess(user, existingUserId, existingReadAccessMode, existingReadClaims);
    }

    private void CheckWritePermissions(IDictionary<string, string> existingMetadata, ClaimsPrincipal user)
    {
        if (!HasWritePermissions(existingMetadata, user))
        {
            throw new UnauthorizedAccessException("User does not have write access to read metadata");
        }
    }

    private bool HasWritePermissions(IDictionary<string, string> existingMetadata, ClaimsPrincipal user)
    {
        existingMetadata.TryGetValue(BuffaloMetadata.WriteClaims, out var writeClaimsJson);
        var existingWriteClaims = writeClaimsJson.FromJson<SecurityClaims>();
        var existingWriteAccessMode = Enum.Parse<AccessLevels>(existingMetadata[BuffaloMetadata.WriteAccessMode]);
        var existingUserId = Guid.Parse(existingMetadata[BuffaloMetadata.UserId]);

        return VerifyAccess(user, existingUserId, existingWriteAccessMode, existingWriteClaims);
    }

    public async Task<FileData> GetFile(Guid id, ClaimsPrincipal? user)
    {
        var file = await _storage.RetrieveFileAsync(id);

        CheckReadPermissions(file.Metadata, user);

        return file;
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

    public async Task<List<FileInfoDto>> RetrieveFileListAsync(bool? includeMetadata, ClaimsPrincipal user)
    {
        var fileList = await _storage.RetrieveFileListAsync();

        var returnList = new List<FileInfoDto>();

        foreach (var file in fileList)
        {
            if (!HasReadPermissions(file.Metadata, user))
            {
                continue;
            }

            var fileDto = file.ToDto();

            if (includeMetadata != true)
            {
                fileDto.Metadata = null;
            }

            returnList.Add(fileDto);
        }

        return returnList;
    }

    public async Task<string?> UpdateFileMetadata(Guid fileId,
        UpdateFileMetadataBody body,
        ClaimsPrincipal user)
    {
        Func<IDictionary<string, string>, IDictionary<string, string>> updateMetadata = existingMetadata =>
        {
            CheckWritePermissions(existingMetadata, user);

            existingMetadata[BuffaloMetadata.ReadAccessMode] = body.ReadAccessLevel.ToString();
            existingMetadata[BuffaloMetadata.WriteAccessMode] = body.WriteAccessLevel.ToString();

            var readClaims = body.ReadSecurityClaims.ToJson();
            if (readClaims is null)
            {
                existingMetadata.Remove(BuffaloMetadata.ReadClaims);
            }
            else
            {
                existingMetadata[BuffaloMetadata.ReadClaims] = readClaims;
            }

            var writeClaims = body.WriteSecurityClaims.ToJson();
            if (writeClaims is null)
            {
                existingMetadata.Remove(BuffaloMetadata.WriteClaims);
            }
            else
            {
                existingMetadata[BuffaloMetadata.WriteClaims] = writeClaims;
            }

            existingMetadata[BuffaloMetadata.UserId] = GetUserClaimsId(user);
            return existingMetadata;
        };

        return await _storage.UpdateFileMetadataAsync(fileId, updateMetadata);
    }


    private static string GetUserClaimsId(ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value ?? "";
    }

    public static bool VerifyAccess(ClaimsPrincipal? requestUser, Guid storedUser, AccessLevels storedAccessLevel,
        SecurityClaims? storedRequiredClaims = null)
    {
        if (requestUser is null)
        {
            return storedAccessLevel == AccessLevels.Public;
        }

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
    public static string? ToJson(this object? obj)
    {
        if (obj is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(obj);
    }

    public static T? FromJson<T>(this string? json)
        where T : class
    {
        if (string.IsNullOrEmpty(json) || json == "null")
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(json);
    }
}