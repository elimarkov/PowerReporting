using PowerTrading.Reporting.Service.Options;
using PowerTrading.Reporting.Service.Models;
using PowerTrading.Reporting.Service.Services;
using Polly;
using Services;

namespace PowerTrading.Reporting.Service.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure service dependencies
/// Follows .NET 8 best practices for dependency injection configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds timer services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddTimerServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register TimeProvider for modern time abstraction
        services.AddSingleton(TimeProvider.System);
        
        return services;
    }

    /// <summary>
    /// Adds reporting services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration to bind options from</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddReportingServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure options from configuration with data annotation validation
        services.AddOptionsWithValidateOnStart<PeriodicTriggerOptions>()
            .Bind(configuration.GetSection(PeriodicTriggerOptions.SectionName))
            .ValidateDataAnnotations();

        // Configure CsvExporterOptions from configuration with data annotation validation
        services.AddOptionsWithValidateOnStart<CsvExporterOptions>()
            .Bind(configuration.GetSection(CsvExporterOptions.SectionName))
            .ValidateDataAnnotations();

        // Register report trigger with its dependencies
        services.AddSingleton<ITrigger, PeriodicTrigger>();
        
        // Register power service (from external library) as singleton for background service
        services.AddSingleton<IPowerService, PowerService>();
        
        // Register mapper as singleton
        services.AddSingleton<IMapper<PowerPeriod, ReportingPowerPeriod>, PowerPeriodMapper>();
        
        // Register report generator as singleton for background service
        services.AddSingleton<IReportGenerator, PowerPositionReportGenerator>();

        // Register CsvReportExporter as IReportExporter
        services.AddSingleton<IReportExporter, CsvReportExporter>();

        services.AddResiliencePipeline("ReportGeneration", (builder, context) =>
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<IntradayPositionReporter>>();
            
            builder
                .AddRetry(new()
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(1),
                    OnRetry = arg =>
                    {
                        logger.LogWarning(arg.Outcome.Exception,
                            "Retry {RetryCount} after {RetryInterval:N0}ms",
                            arg.AttemptNumber,
                            arg.RetryDelay.TotalMilliseconds);
                        return default;
                    }
                });
        });        
        
        return services;
    }

    /// <summary>
    /// Adds all application services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration to bind options from</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddTimerServices();
        services.AddReportingServices(configuration);

        return services;
    }
}