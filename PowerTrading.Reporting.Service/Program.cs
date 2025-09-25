using PowerTrading.Reporting.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<IntradayPositionReporter>();

// Add Windows service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Petroineos PowerTrading Reporting Service";
});

var host = builder.Build();
host.Run();
