using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Buffalo.Models;
using Buffalo.Options;
using Buffalo.Utils;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Buffalo.Implementations
{
	public class AmazonS3 : IStorage
	{

		private readonly AmazonS3Client s3Client;

		private readonly string FolderName;
		private readonly string BucketName;

		public AmazonS3(IOptions<S3Options> options)
		{
			var s3Credential = new BasicAWSCredentials(options.Value.AccessKey, options.Value.SecretKey);
			var s3Config = new AmazonS3Config
			{
				RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Value.RegionEndpoint)
			};
			FolderName = options.Value.FolderName ?? "default";
			BucketName = options.Value.BucketName ?? "default";

			s3Client = new AmazonS3Client(s3Credential, s3Config);

		}

		public async Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage, AccessModes accessMode)
		{
			try
			{
				var bucketName = !string.IsNullOrWhiteSpace(FolderName)
					? BucketName + @"/" + FolderName
					: BucketName;



				var publicUri = $"https://{bucketName}.s3.amazonaws.com/{fileNameForStorage}";

				var uploadRequest = new TransferUtilityUploadRequest
				{
					InputStream = imageFile.OpenReadStream(),
					Key = fileNameForStorage,
					ContentType = imageFile.ContentType,
					BucketName = bucketName,
					CannedACL = (accessMode == AccessModes.PUBLIC) ? S3CannedACL.PublicRead : S3CannedACL.Private
				};


				var fileTransferUtility = new TransferUtility(s3Client);
				await fileTransferUtility.UploadAsync(uploadRequest);

				return publicUri;

			}
			catch (AmazonS3Exception amazonS3Exception)
			{
				if (amazonS3Exception.ErrorCode != null
					&& (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					return "Check the provided AWS Credentials.";
					//throw new Exception("Check the provided AWS Credentials.");
				}
				else
				{
					return amazonS3Exception.Message;
					//throw new Exception("Error occurred: " + amazonS3Exception.Message);
				}
			}

		}

		public async Task DeleteFileAsync(string fileNameForStorage)
		{
			try
			{

				var fileTransferUtility = new TransferUtility(s3Client);
				await fileTransferUtility.S3Client.DeleteObjectAsync(new DeleteObjectRequest()
				{
					BucketName = BucketName,
					Key = fileNameForStorage
				});
			}

			catch (AmazonS3Exception amazonS3Exception)
			{
				if (amazonS3Exception.ErrorCode != null
					&& (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					throw new Exception("Check the provided AWS Credentials.");
				}
				else if (amazonS3Exception.ErrorCode != null && amazonS3Exception.ErrorCode.Equals("NoSuchKey"))
				{
					throw new Exception(amazonS3Exception.ErrorCode);
				}
				else
				{
					throw new Exception("Error occurred: " + amazonS3Exception.Message);
				}

			}

		}

		public async Task<Stream> RetrieveFileAsync(Guid id)

		{
			try
			{
				var fileTransferUtility = new TransferUtility(s3Client);

				var objectResponse = await fileTransferUtility.S3Client.GetObjectAsync(new GetObjectRequest()
				{
					BucketName = BucketName,
					Key = id.ToString()
				});

				if (objectResponse.ResponseStream == null)
				{
					throw new Exception("File not exists.");
				}

				return objectResponse.ResponseStream;
			}
			catch (AmazonS3Exception amazonS3Exception)
			{
				if (amazonS3Exception.ErrorCode != null
					&& (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
				{
					throw new Exception("Check the provided AWS Credentials.");
				}
				else if (amazonS3Exception.ErrorCode != null && amazonS3Exception.ErrorCode.Equals("NoSuchKey"))
				{
					throw new Exception(amazonS3Exception.ErrorCode);
				}
				else
				{
					throw new Exception("Error occurred: " + amazonS3Exception.Message);
				}
			}


		}

	}
}