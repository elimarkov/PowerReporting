using PowerTrading.Reporting.Service;
using Serilog;

// Create bootstrap logger for early startup logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/powertrading-reporting-bootstrap-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateBootstrapLogger();

try
{
    Log.Information("Configuring PowerTrading Reporting Service");
    
    var builder = Host.CreateApplicationBuilder(args);
    
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));
    
    builder.Services.AddHostedService<IntradayPositionReporter>();

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Petroineos PowerTrading Reporting Service";
    });

    var host = builder.Build();
    
    Log.Information("PowerTrading Reporting Service configured successfully");
    
    Log.Information("Starting PowerTrading Reporting Service");
    await host.RunAsync();
    Log.Information("PowerTrading Reporting Service stopped");
}
catch (Exception ex)
{
    Log.Fatal(ex, "PowerTrading Reporting Service terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
