using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
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
	public class AmazonS3 : IStorage
	{
		private string FolderName { get; }
		private string BucketName { get; }
		private string AccessKey { get; }
		private string SecretKey { get; }

		public AmazonS3(IConfiguration configuration)
		{
			FolderName = configuration.GetValue<string>("AwsSettings:FolderName");
			BucketName = configuration.GetValue<string>("AwsSettings:BucketName");
			AccessKey = configuration.GetValue<string>("AwsSettings:AccessKey");
			SecretKey = configuration.GetValue<string>("AwsSettings:SecretKey");
		}

		public async Task<string> UploadFileAsync(IFormFile imageFile, string fileNameForStorage, AccessModes accessMode)
		{
			try
			{
				var bucketName = !string.IsNullOrWhiteSpace(FolderName)
					? BucketName + @"/" + FolderName
					: BucketName;

				var credentials = new BasicAWSCredentials(AccessKey, SecretKey);
				var config = new AmazonS3Config
				{
					RegionEndpoint = Amazon.RegionEndpoint.EUWest3
				};

				using var client = new AmazonS3Client(credentials, config);

				var documentName = Guid.NewGuid();

				var publicUri = $"https://{bucketName}.s3.amazonaws.com/{documentName}";

				var uploadRequest = new TransferUtilityUploadRequest
				{
					InputStream = imageFile.OpenReadStream(),
					Key = documentName.ToString(),
					ContentType = imageFile.ContentType,
					BucketName = bucketName,
					CannedACL = (accessMode == AccessModes.PUBLIC) ? S3CannedACL.PublicRead : S3CannedACL.Private
				};


				var fileTransferUtility = new TransferUtility(client);
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

				var credentials = new BasicAWSCredentials(AccessKey, SecretKey);
				var config = new AmazonS3Config
				{
					RegionEndpoint = Amazon.RegionEndpoint.EUWest3
				};
				using var client = new AmazonS3Client(credentials, config);
				var fileTransferUtility = new TransferUtility(client);
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
				var credentials = new BasicAWSCredentials(AccessKey, SecretKey);
				var config = new AmazonS3Config
				{
					RegionEndpoint = Amazon.RegionEndpoint.EUWest3
				};
				using var client = new AmazonS3Client(credentials, config);
				var fileTransferUtility = new TransferUtility(client);

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