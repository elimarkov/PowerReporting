using PowerTrading.Reporting.Service.Options;

namespace PowerTrading.Reporting.Service.Services;

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

        // Configure options from configuration with fallback defaults
        services.Configure<PeriodicTriggerOptions>(
            configuration.GetSection(PeriodicTriggerOptions.SectionName));

        // Register report trigger with its dependencies
        services.AddSingleton<ITrigger, PeriodicTrigger>();
        
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