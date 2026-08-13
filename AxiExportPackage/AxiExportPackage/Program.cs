using Serilog;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using AxiExportPackage.Consumers;
using AxiExportPackage.Services;
using AxiExportPackage.Services.Interfaces;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/AxiExportPack_Log_.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();

try
{
    Log.Information("Application starting...");

    string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    if (!File.Exists(appSettingsPath))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: '{appSettingsPath}' was not found.");
        Console.WriteLine("Application cannot continue without appsettings.json. Press enter to close.");
        Console.ResetColor();
        Console.ReadLine();
        return;
    }

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddHostedService<RabbitMqConsumer>();

    builder.Services.AddScoped<SignalRClass>();

    builder.Services.AddScoped<
        IPackageExportService,
        PackageExportService>();

    builder.Services.AddHttpClient<ApiService>();

    builder.Services.AddSerilog();

    var host = builder.Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
    Log.Information("Press enter to close the console.");
    Console.ReadLine();
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