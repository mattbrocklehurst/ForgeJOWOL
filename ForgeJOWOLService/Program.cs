using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ForgeJOWOLService;

// 1. Create the builder
var builder = Host.CreateApplicationBuilder(args);

// 2. Configure automatically pulls from appsettings.json 
// (Host.CreateApplicationBuilder handles this for you by default)
builder.Services.Configure<ForgejoOptions>(
    builder.Configuration.GetSection("ForgejoSettings"));

// 3. Register your service
builder.Services.AddHostedService<Worker>();

// 4. Build and Run
using IHost host = builder.Build();
host.Run();