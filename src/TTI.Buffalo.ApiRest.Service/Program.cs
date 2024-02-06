using Buffalo.Sample;
using Google.Cloud.Logging.Console;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json.Serialization;
using TTI.Buffalo;
using TTI.Buffalo.AmazonS3;
using TTI.Buffalo.GoogleCloud;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.


if (builder.Environment.IsProduction())
{
    string? path = Environment.GetEnvironmentVariable("CONFIG_PATH");
    if (string.IsNullOrEmpty(path))
    {
        throw new ArgumentException("CONFIG_PATH in non Development Environment can NOT be null");
    }

    string? cloud_env = Environment.GetEnvironmentVariable("CLOUD_ENV");
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

    builder.Configuration.AddJsonFile(path, optional: false, reloadOnChange: true);
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

Dictionary<string, object>? settings = builder.Configuration
  .GetSection("BuffaloSettings:GoogleCredentialFile")
  .Get<Dictionary<string, object>>();
string json = JsonConvert.SerializeObject(settings);

string? system = builder.Configuration.GetSection("BuffaloSettings:AvailableSystem").Value;


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

        // JWT tokens
        .AddJwtBearer("token", options =>
        {

            options.Authority = passportOptions.Authority;
            options.Audience = passportOptions.Audience;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                NameClaimType = "sub",
                RoleClaimType = "role",
            };

            options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

            // if token does not contain a dot, it is a reference token
            options.ForwardDefaultSelector = Selector.ForwardReferenceToken("introspection");
        })

        // reference tokens
        .AddOAuth2Introspection("introspection", options =>
        {
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
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Buffalo API",
        Description = "Buffalo allows Upload your Favorite Files via this Awesome Library for Objects",
        Contact = new OpenApiContact
        {
            Name = "Source",
            Url = new Uri("https://github.com/TTiVentures/Buffalo")
        },
        License = new OpenApiLicense
        {
            Name = "License",
            Url = new Uri("https://github.com/TTiVentures/Buffalo/blob/main/LICENSE")
        }
    });

    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if ((app.Configuration.GetValue<bool?>("BuffaloSettings:UseSwagger") ?? false) == true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
