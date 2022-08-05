using Buffalo;
using Buffalo.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    var enumConverter = new JsonStringEnumConverter();
    opts.JsonSerializerOptions.Converters.Add(enumConverter);
});



builder.Services.AddSingleton<IStorage, AmazonS3>();

//builder.Services.AddSingleton<IStorage, GoogleCloudStorage>();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = "https://localhost:5001";

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
        policy.RequireClaim("scope", "static");
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
