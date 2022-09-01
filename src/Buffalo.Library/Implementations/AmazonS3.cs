using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Buffalo.Models;
using Buffalo.Options;
using Buffalo.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

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

        public async Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage, AccessModes accessMode, string? user)
        {
            try
            {
                var bucketName = !string.IsNullOrWhiteSpace(FolderName)
                    ? BucketName + @"/" + FolderName
                    : BucketName;



                var publicUri = $"https://{bucketName}.s3.amazonaws.com/{fileNameForStorage}";

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = file.OpenReadStream(),
                    Key = fileNameForStorage,
                    ContentType = file.ContentType ?? MimeTypeTool.GetMimeType(file.FileName),
                    BucketName = bucketName,
                    CannedACL = (accessMode == AccessModes.PUBLIC) ? S3CannedACL.PublicRead : S3CannedACL.Private
                };

                uploadRequest.Metadata.Add("buffalo_user", user);
                uploadRequest.Metadata.Add("buffalo_accessmode", accessMode.ToString());
                uploadRequest.Metadata.Add("buffalo_filename", file.FileName);


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

        public async Task DeleteFileAsync(Guid id, string? user)
        {
            try
            {

                var obj = await s3Client.GetObjectAsync(BucketName, id.ToString());

                if (SecurityTool.VerifyAccess(user, obj.Metadata["buffalo_user"], obj.Metadata["buffalo_accessmode"]) == false)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
                }

                var fileTransferUtility = new TransferUtility(s3Client);
                await fileTransferUtility.S3Client.DeleteObjectAsync(new DeleteObjectRequest()
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
                var obj = await s3Client.GetObjectAsync(BucketName, id.ToString());

                if (SecurityTool.VerifyAccess(user, obj.Metadata["buffalo_user"], obj.Metadata["buffalo_accessmode"]) == false)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to PROTECTED resource");
                }

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

                return new FileData(objectResponse.ResponseStream, obj.Metadata["buffalo_filename"], obj.Headers["Content-Type"]);
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