using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json.Serialization;
using Buffalo.Sample;
using Google.Cloud.Logging.Console;
using Newtonsoft.Json;
using TTI.Buffalo;
using TTI.Buffalo.AmazonS3;
using TTI.Buffalo.GoogleCloud;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

if (builder.Environment.IsProduction())
{
    var path = Environment.GetEnvironmentVariable("CONFIG_PATH");
    if (string.IsNullOrEmpty(path))
    {
        throw new ArgumentException("CONFIG_PATH in non Development Environment can NOT be null");
    }

    var cloud_env = Environment.GetEnvironmentVariable("CLOUD_ENV");
    if (!string.IsNullOrEmpty(cloud_env))
    {
        if (cloud_env == "AWS")
        {
            builder.Logging.AddJsonConsole();
        }
        else if (cloud_env == "GCP")
        {
            builder.Logging.AddGoogleCloudConsole();
        }
    }

    builder.Configuration.AddJsonFile(path, false, true);
}

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    JsonStringEnumConverter enumConverter = new();
    opts.JsonSerializerOptions.Converters.Add(enumConverter);
});

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

PassportOptions passportOptions = new();
builder.Configuration.GetSection("BuffaloSettings:Passport").Bind(passportOptions);

S3Options amazonOptions = new();
builder.Configuration.GetSection("BuffaloSettings:AWSCredentials").Bind(amazonOptions);

var settings = builder.Configuration
    .GetSection("BuffaloSettings:GoogleCredentialFile")
    .Get<Dictionary<string, object>>();
var json = JsonConvert.SerializeObject(settings);

var system = builder.Configuration.GetSection("BuffaloSettings:AvailableSystem").Value;

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
        default:
            throw new ArgumentException("No storage system has been configured.");
    }
});

if (passportOptions.RequireAuthentication)
{
    builder.Services.AddAuthentication("token")
        .AddJwtBearer("token", options =>
        {
            // JWT tokens  
            options.Authority = passportOptions.Authority;
            options.Audience = passportOptions.Audience;

            options.TokenValidationParameters = new()
            {
                ValidateAudience = true,
                NameClaimType = "sub",
                RoleClaimType = "role"
            };

            options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

            // if token does not contain a dot, it is a reference token
            options.ForwardDefaultSelector = Selector.ForwardReferenceToken();
        })
        .AddOAuth2Introspection("introspection", options =>
        {
            // reference tokens  
            options.Authority = passportOptions.Authority;

            options.ClientId = passportOptions.ClientId;
            options.ClientSecret = passportOptions.ClientSecret;

            options.NameClaimType = "sub";
            options.RoleClaimType = "role";
        });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Version = "v1",
        Title = "Buffalo API",
        Description = "Buffalo allows Upload your Favorite Files via this Awesome Library for Objects",
        Contact = new()
        {
            Name = "Source",
            Url = new("https://github.com/TTiVentures/Buffalo")
        },
        License = new()
        {
            Name = "License",
            Url = new("https://github.com/TTiVentures/Buffalo/blob/main/LICENSE")
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Configuration.GetValue<bool?>("BuffaloSettings:UseSwagger") ?? false)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

var controllers = app.MapControllers();

if (passportOptions.RequireAuthentication)
{
    controllers.RequireAuthorization();
}

app.Run();