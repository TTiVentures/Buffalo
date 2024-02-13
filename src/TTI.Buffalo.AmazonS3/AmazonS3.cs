﻿using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using TTI.Buffalo.Models;

namespace TTI.Buffalo.AmazonS3
{
    public class AmazonS3 : IStorage
	{
		private readonly AmazonS3Client s3Client;

		private readonly string _folderName;
		private readonly string _bucketName;

		public AmazonS3(IOptions<S3Options> options)
		{
			BasicAWSCredentials? s3Credential = new(options.Value.AccessKey, options.Value.SecretKey);
			AmazonS3Config? s3Config = new()
			{
				RegionEndpoint = RegionEndpoint.GetBySystemName(options.Value.RegionEndpoint)
			};
			_folderName = options.Value.FolderName ?? "default";
			_bucketName = options.Value.BucketName ?? "default";

			s3Client = new AmazonS3Client(s3Credential, s3Config);
		}

		public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage, 
			AccessLevels accessLevel, ClaimsPrincipal? user, RequiredClaims? requiredClaims)
		{


            string? userId = null;
            if (user != null && user.Identity != null)
            {
                userId = user.Identity.Name;
            }

            try
			{
				string? bucketName = !string.IsNullOrWhiteSpace(_folderName)
					? _bucketName + @"/" + _folderName
					: _bucketName;

				string? publicUri = $"https://{bucketName}.s3.amazonaws.com/{fileNameForStorage}";

                /*

                var al = MD5.Create();
                using var cs = new CryptoStream(file.OpenReadStream(), al, CryptoStreamMode.Read);
			

				using MemoryStream memStream = new MemoryStream();
                
                    cs.CopyTo(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);
				*/

                TransferUtilityUploadRequest? uploadRequest = new()
				{
					InputStream = file.OpenReadStream(),
                    //InputStream = memStream,
					Key = fileNameForStorage,
					ContentType = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName),
					BucketName = bucketName,
					CannedACL = accessLevel == AccessLevels.Public ? S3CannedACL.PublicRead : S3CannedACL.Private
				};

				uploadRequest.Metadata.Add(MetadataConst.BUFFALO_USER_ID, userId);
				uploadRequest.Metadata.Add(MetadataConst.BUFFALO_ACCESS_MODE, accessLevel.ToString());
                uploadRequest.Metadata.Add(MetadataConst.BUFFALO_FILENAME, file.FileName);


                if (accessLevel == AccessLevels.Claims)
                {
                    if (requiredClaims != null)
                    {
                        uploadRequest.Metadata.Add(MetadataConst.BUFFALO_REQUIRED_CLAIMS, JsonSerializer.Serialize(requiredClaims));
                    }
                    else
                    {
                        throw new ArgumentException("RequiredClaims must not be empty in CLAIMS level access");
                    }

                }


				TransferUtility? fileTransferUtility = new(s3Client);
				await fileTransferUtility.UploadAsync(uploadRequest);

				//var hash = BitConverter.ToString(al.Hash!);

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

		public async Task DeleteFileAsync(Guid id, ClaimsPrincipal? user)
		{
			try
			{

				GetObjectResponse? obj = await s3Client.GetObjectAsync(_bucketName, id.ToString());

				if (SecurityTool.VerifyAccess(user, obj.Metadata[MetadataConst.BUFFALO_USER_ID], 
					obj.Metadata[MetadataConst.BUFFALO_ACCESS_MODE], obj.Metadata[MetadataConst.BUFFALO_REQUIRED_CLAIMS]) == false)
				{
					throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
				}

				TransferUtility? fileTransferUtility = new(s3Client);
				_ = await fileTransferUtility.S3Client.DeleteObjectAsync(new DeleteObjectRequest()
				{
					BucketName = _bucketName,
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

		public async Task<FileData> RetrieveFileAsync(Guid id, ClaimsPrincipal? user)
		{
			try
			{
				GetObjectResponse? obj = await s3Client.GetObjectAsync(_bucketName, id.ToString());

				if (SecurityTool.VerifyAccess(user, obj.Metadata[MetadataConst.BUFFALO_USER_ID], 
					obj.Metadata[MetadataConst.BUFFALO_ACCESS_MODE], obj.Metadata[MetadataConst.BUFFALO_REQUIRED_CLAIMS]) == false)
				{
					throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
				}

				TransferUtility? fileTransferUtility = new(s3Client);

				GetObjectResponse? objectResponse = await fileTransferUtility.S3Client.GetObjectAsync(new GetObjectRequest()
				{
					BucketName = _bucketName,
					Key = id.ToString()
				});

				return objectResponse.ResponseStream == null
					? throw new Exception("File not exists.")
					: new FileData(objectResponse.ResponseStream, obj.Metadata[MetadataConst.BUFFALO_FILENAME], obj.Headers["Content-Type"]);
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

        public async Task<FileInfoList> RetrieveFileListAsync(bool? includeMetadata)
		{

			var request = new ListObjectsV2Request
			{
				BucketName = _bucketName,

			};
            ListObjectsV2Response? result = await s3Client.ListObjectsV2Async(request);

			var response = new FileInfoList();


			if (result != null) {

                foreach (var item in result.S3Objects)
                {
					response.Objects.Add(item.Key);
                }
				response.Total = result.KeyCount;
            }

            return response;
        }


    }
}