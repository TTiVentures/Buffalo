using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Buffalo.Library.Models;
using Buffalo.Library.Options;
using Buffalo.Library.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Buffalo.Library.Implementations
{
	public class AmazonS3 : IStorage
	{
		private readonly AmazonS3Client s3Client;

		private readonly string FolderName;
		private readonly string BucketName;

		public AmazonS3(IOptions<S3Options> options)
		{
			BasicAWSCredentials? s3Credential = new(options.Value.AccessKey, options.Value.SecretKey);
			AmazonS3Config? s3Config = new()
			{
				RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Value.RegionEndpoint)
			};
			FolderName = options.Value.FolderName ?? "default";
			BucketName = options.Value.BucketName ?? "default";

			s3Client = new AmazonS3Client(s3Credential, s3Config);
		}

		public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage, AccessLevels accessLevel, string? user)
		{
			try
			{
				string? bucketName = !string.IsNullOrWhiteSpace(FolderName)
					? BucketName + @"/" + FolderName
					: BucketName;

				string? publicUri = $"https://{bucketName}.s3.amazonaws.com/{fileNameForStorage}";

				TransferUtilityUploadRequest? uploadRequest = new()
				{
					InputStream = file.OpenReadStream(),
					Key = fileNameForStorage,
					ContentType = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName),
					BucketName = bucketName,
					CannedACL = accessLevel == AccessLevels.PUBLIC ? S3CannedACL.PublicRead : S3CannedACL.Private
				};

				uploadRequest.Metadata.Add("buffalo_user", user);
				uploadRequest.Metadata.Add("buffalo_accessmode", accessLevel.ToString());
				uploadRequest.Metadata.Add("buffalo_filename", file.FileName);

				TransferUtility? fileTransferUtility = new(s3Client);
				await fileTransferUtility.UploadAsync(uploadRequest);

				return publicUri;
			}
			catch (AmazonS3Exception amazonS3Exception)
			{
				return amazonS3Exception.ErrorCode != null
					&& (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity"))
					? "Check the provided AWS Credentials."
					: amazonS3Exception.Message;
			}
		}

		public async Task DeleteFileAsync(Guid id, string? user)
		{
			try
			{

				GetObjectResponse? obj = await s3Client.GetObjectAsync(BucketName, id.ToString());

				if (SecurityTool.VerifyAccess(user, obj.Metadata["buffalo_user"], obj.Metadata["buffalo_accessmode"]) == false)
				{
					throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
				}

				TransferUtility? fileTransferUtility = new(s3Client);
				_ = await fileTransferUtility.S3Client.DeleteObjectAsync(new DeleteObjectRequest()
				{
					BucketName = BucketName,
					Key = id.ToString()
				});
			}
			catch (AmazonS3Exception amazonS3Exception)
			{
				if (amazonS3Exception.ErrorCode != null
					&& (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					throw new UnauthorizedAccessException("Provided Amazon S3 credentials do not have access to this resource.");
				}
				else if (amazonS3Exception.ErrorCode != null && amazonS3Exception.ErrorCode.Equals("NoSuchKey"))
				{
					throw new FileNotFoundException(amazonS3Exception.ErrorCode);
				}
				else
				{
					throw new Exception("Error occurred: " + amazonS3Exception.Message);
				}
			}
		}

		public async Task<FileData> RetrieveFileAsync(Guid id, string? user)
		{
			try
			{
				GetObjectResponse? obj = await s3Client.GetObjectAsync(BucketName, id.ToString());

				if (SecurityTool.VerifyAccess(user, obj.Metadata["buffalo_user"], obj.Metadata["buffalo_accessmode"]) == false)
				{
					throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
				}

				TransferUtility? fileTransferUtility = new(s3Client);

				GetObjectResponse? objectResponse = await fileTransferUtility.S3Client.GetObjectAsync(new GetObjectRequest()
				{
					BucketName = BucketName,
					Key = id.ToString()
				});

				return objectResponse.ResponseStream == null
					? throw new Exception("File not exists.")
					: new FileData(objectResponse.ResponseStream, obj.Metadata["buffalo_filename"], obj.Headers["Content-Type"]);
			}
			catch (AmazonS3Exception amazonS3Exception)
			{
				if (amazonS3Exception.ErrorCode != null
					&& (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					throw new UnauthorizedAccessException("Provided Amazon S3 credentials do not have access to this resource.");
				}
				else if (amazonS3Exception.ErrorCode != null && amazonS3Exception.ErrorCode.Equals("NoSuchKey"))
				{
					throw new FileNotFoundException(amazonS3Exception.ErrorCode);
				}
				else
				{
					throw new Exception("Error occurred: " + amazonS3Exception.Message);
				}
			}
		}
	}
}