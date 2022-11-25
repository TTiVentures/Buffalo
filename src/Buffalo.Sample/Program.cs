using Buffalo.Sample;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using TTI.Buffalo.AmazonS3;
using TTI.Buffalo;
using TTI.Buffalo.GoogleCloud;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

if (builder.Environment.IsProduction())
{
    string? path = Environment.GetEnvironmentVariable("CONFIG_PATH");
    if (path == null)
    {
        throw new ArgumentException("CONFIG_PATH in non Development Environment can NOT be null");
    }
    builder.Configuration.AddJsonFile(path, optional: false, reloadOnChange: true);
}

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    var enumConverter = new JsonStringEnumConverter();
    opts.JsonSerializerOptions.Converters.Add(enumConverter);
});


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var passportOptions = new PassportOptions();
builder.Configuration.GetSection("BuffaloSettings:Passport").Bind(passportOptions);


var amazonOptions = new S3Options();
builder.Configuration.GetSection("BuffaloSettings:AWSCredentials").Bind(amazonOptions);

Dictionary<string, object> settings = builder.Configuration
  .GetSection("BuffaloSettings:GoogleCredentialFile")
  .Get<Dictionary<string, object>>();
string json = JsonConvert.SerializeObject(settings);

string system = builder.Configuration.GetSection("BuffaloSettings:AvailableSystem").Value;


builder.Services.AddBuffalo(x =>
{

    switch (system)
    {
        case "GCS":
            x.UseCloudStorage(z =>
            {
                z.JsonCredentialsFile = json;
                z.StorageBucket = builder.Configuration.GetValue<string>("BuffaloSettings:GoogleCloudStorageBucket");
            });
            break;

        case "S3":
            x.UseAmazonS3(y =>
            {
                y.AccessKey = amazonOptions.AccessKey;
                y.SecretKey = amazonOptions.SecretKey;
                y.BucketName = amazonOptions.BucketName;
                y.FolderName = amazonOptions.FolderName;
                y.RegionEndpoint = amazonOptions.RegionEndpoint;
            });
            break;
    }
});

if (passportOptions.RequireAuthentication) { 

    builder.Services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = passportOptions.Authority;

					options.Audience = passportOptions.Audience;

					options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        NameClaimType = "sub",
                        RoleClaimType = "role",
                    };
                });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if ((app.Configuration.GetValue<bool?>("BuffaloSettings:UseSwagger") ?? false) == true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


if (passportOptions.RequireAuthentication)
{

    app.MapControllers().RequireAuthorization("ApiScope");
}
else
{
    app.MapControllers();
}

app.Run();
