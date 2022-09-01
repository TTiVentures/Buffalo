<p align="center">
  <img src="https://i.imgur.com/9OVaKpe.png">
</p>

_by [TTI Ventures](https://ttiventures.com/)_

------------------------------------------------------------------------------

**B**uffalo allows **U**pload your **F**avorite **F**iles via this **A**wesome **L**ibrary for **O**bjects 

Buffalo is a library for .NET that ease the management of static objects and files in the cloud by abstracting each service's own libraries and presenting a simple and lightweight common interface.

Additionally, Buffalo adds a security layer that allows you to control access or deletion of your files, as well as distribute them publicly through the cloud CDN.

At the moment only [Google Cloud Storage](https://cloud.google.com/storage) and [Amazon S3](https://aws.amazon.com/s3/) are supported.

## Features

- Static object management in the cloud (Upload, Delete, Download).
- Multi-cloud implementation (Cloud Storage and Amazon S3).
- Per-user object access level.
- Automatic public ACL management.
- Easy configuration.
- No database or local storage required.

## Setup

Use the *AddBuffalo* extension method for *IServiceCollection* to configure the library host upon startup of your application. Depending on the cloud storage service you want to use, different parameters must be provided.

### Google Cloud Storage

The following parameters are required to use Google Cloud Storage:

* ```JsonCredentialsFile```: Google Cloud service account key in JSON format.
* ```StorageBucket```: Google Cloud Storage bucket in which the library will operate.

```c#
services.AddBuffalo(x =>
{    
    x.UseCloudStorage(h =>
    {
        h.JsonCredentialsFile = <SERVICE_ACCOUNT_KEY>;
        h.StorageBucket = <STORAGE_BUCKET_NAME>;
    });
});
```

### Amazon S3

The following parameters are required to use Amazon S3:

* ```AccessKey```: ID key of your AWS Credentials.
* ```SecretKey```: Secret key of your AWS Credentials.
* ```BucketName```: Amazon S3 bucket in which the library will operate.
* ```FolderName```: Folder inside bucket in in which the library will operate.
* ```RegionEndpoint```: AWS Region by system name in which the library will operate (like "us-west-1").

```c#
services.AddBuffalo(x =>
{
    x.UseAmazonS3(y =>
    {
        y.AccessKey = <AWS_ID_KEY>;
        y.SecretKey = <AWS_SECRET_KEY>;
        y.BucketName = <S3_BUCKET_NAME>;
        y.FolderName = <FOLDER_NAME>;
        y.RegionEndpoint = <AWS_S3_REGION>;
    });
});
```

## Usage

When your application starts, grab the Buffalo library from the built-in dependency injection and you will be able to use the **FileManager** from any class, controller, etc...

```c#
private readonly FileManager _fileManager;

public MyFunction(FileManager fileManager)
{
    _fileManager = fileManager;
}
```

An [example project](src/Buffalo.Sample/Controllers/FileController.cs) is included in which the library is used as part of a controller.

### Upload File
* ```file (IFormFile)```: File to be upload in *IFormFile* structure.
* ```user (string?)```: User id uploading the file.
* ```accessLevel (AccessLevels)```: `PUBLIC`, `PRIVATE` or `PROTECTED` type access level in *AccessLevels* enum.
```c#
await _fileManager.UploadFile(file, user, accessLevel);
```

### Delete File
* ```id (Guid)```: Id of the object to be deleted.
* ```user (string?)```: User id deleting the file.
```c#
bool deleted = await _fileManager.DeleteFile(id, user);
```

### Download File
* ```id (Guid)```: Id of the object to be retrieved.
* ```user (string?)```: User id downloading the file.
```c#
var file = await _fileManager.GetFile(id, user);
```
