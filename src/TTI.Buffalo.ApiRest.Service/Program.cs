using TTI.Buffalo.ApiRest.Service;

var builder = WebApplication.CreateBuilder(args);

builder.CloudBuffaloConfigureServices();

var app = builder.Build();

await app.CloudBuffaloConfigureApplication();

app.Run();