using Buffalo;
using Buffalo.Extensions.DependencyInjection;
using Buffalo.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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


builder.Services.AddBuffalo(x =>
{
    /*
    x.UseAmazonS3(y =>
    {
        y.AccessKey = amazonOptions.AccessKey;
        y.SecretKey = amazonOptions.SecretKey;
        y.BucketName = amazonOptions.BucketName;
        y.FolderName = amazonOptions.FolderName;
    });
    */

    x.UseCloudStorage(z =>
    {
        z.JsonCredentialsFile = json;
        z.StorageBucket = builder.Configuration.GetValue<string>("BuffaloSettings:GoogleCloudStorageBucket");
    });

});


builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = passportOptions.Authority;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };
            });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();

        if (passportOptions.RequiredClaim != null)
        {
            policy.RequireClaim("scope", passportOptions.RequiredClaim);
        }

    });
});



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle


builder.Services.AddDbContext<FileContext>(options => options.UseSqlite($"Data Source=./files.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization("ApiScope");

app.Run();
