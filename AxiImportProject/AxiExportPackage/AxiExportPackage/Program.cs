using Serilog;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using AxiInstallConsumerPackage.Consumers;
using AxiInstallConsumerPackage.Services;
using AxiInstallConsumerPackage.Helpers;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/AxiInstallPack_Log_.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();

try
{
    Log.Information("Application starting...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddHostedService<RabbitMqConsumer>();

    builder.Services.AddScoped<SignalRClass>();

    builder.Services.AddScoped<
        IPackageImportService,
        PackageImportService>();

    builder.Services.AddHttpClient<ApiService>();

    builder.Services.AddSerilog();

    var host = builder.Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

/*using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using AxiExportPackage.Consumers;
using AxiExportPackage.Services;
using AxiExportPackage.Services.Interfaces;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<RabbitMqConsumer>();

builder.Services.AddScoped<
    IPackageExportService,
    PackageExportService>();

builder.Services.AddHttpClient<ApiService>();

var host = builder.Build();

host.Run();*/